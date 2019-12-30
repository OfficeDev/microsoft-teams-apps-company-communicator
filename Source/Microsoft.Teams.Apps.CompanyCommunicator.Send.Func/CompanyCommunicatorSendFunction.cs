// <copyright file="CompanyCommunicatorSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for sending messages from the bot.
    /// </summary>
    public class CompanyCommunicatorSendFunction
    {
        private const string SendQueueName = "company-communicator-send";

        private static readonly int MaxDeliveryCountForDeadLetter = 10;

        private static HttpClient httpClient = null;

        private static SendingNotificationDataRepository sendingNotificationDataRepository = null;

        private static GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository = null;

        private static UserDataRepository userDataRepository = null;

        private static SentNotificationDataRepository sentNotificationDataRepository = null;

        private static string botAccessToken = null;

        private static DateTime? botAccessTokenExpiration = null;

        private static MessageSender sendQueueServiceBusMessageSender = null;

        private static IConfiguration configuration = null;

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for sending messages from the bot.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="deliveryCount">The deliver count.</param>
        /// <param name="enqueuedTimeUtc">The enqueued time.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="log">The logger.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorSendFunction")]
        public async Task Run(
            [ServiceBusTrigger(CompanyCommunicatorSendFunction.SendQueueName, Connection = "ServiceBusConnection")]
            string myQueueItem,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            CompanyCommunicatorSendFunction.configuration = CompanyCommunicatorSendFunction.configuration ??
                new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();

            var messageContent = JsonConvert.DeserializeObject<ServiceBusSendQueueMessageContent>(myQueueItem);

            var totalNumberOfThrottles = 0;

            try
            {
                // Simply initialize the variable for certain build environments and versions
                var maxNumberOfAttempts = 0;

                // If parsing fails, out variable is set to 0, so need to set the default
                if (!int.TryParse(CompanyCommunicatorSendFunction.configuration["MaxNumberOfAttempts"], out maxNumberOfAttempts))
                {
                    maxNumberOfAttempts = 1;
                }

                CompanyCommunicatorSendFunction.httpClient = CompanyCommunicatorSendFunction.httpClient
                    ?? new HttpClient();

                CompanyCommunicatorSendFunction.userDataRepository = CompanyCommunicatorSendFunction.userDataRepository
                    ?? new UserDataRepository(CompanyCommunicatorSendFunction.configuration, new RepositoryOptions { IsAzureFunction = true });

                CompanyCommunicatorSendFunction.sendingNotificationDataRepository = CompanyCommunicatorSendFunction.sendingNotificationDataRepository
                    ?? new SendingNotificationDataRepository(CompanyCommunicatorSendFunction.configuration, new RepositoryOptions { IsAzureFunction = true });

                CompanyCommunicatorSendFunction.globalSendingNotificationDataRepository = CompanyCommunicatorSendFunction.globalSendingNotificationDataRepository
                    ?? new GlobalSendingNotificationDataRepository(CompanyCommunicatorSendFunction.configuration, isFromAzureFunction: true);

                CompanyCommunicatorSendFunction.sentNotificationDataRepository = CompanyCommunicatorSendFunction.sentNotificationDataRepository
                    ?? new SentNotificationDataRepository(CompanyCommunicatorSendFunction.configuration, new RepositoryOptions { IsAzureFunction = true });

                if (CompanyCommunicatorSendFunction.botAccessToken == null
                    || CompanyCommunicatorSendFunction.botAccessTokenExpiration == null
                    || DateTime.UtcNow > CompanyCommunicatorSendFunction.botAccessTokenExpiration)
                {
                    await this.FetchTokenAsync(CompanyCommunicatorSendFunction.configuration, CompanyCommunicatorSendFunction.httpClient);
                }

                CompanyCommunicatorSendFunction.sendQueueServiceBusMessageSender = CompanyCommunicatorSendFunction.sendQueueServiceBusMessageSender
                    ?? new MessageSender(CompanyCommunicatorSendFunction.configuration["ServiceBusConnection"], CompanyCommunicatorSendFunction.SendQueueName);

                var getActiveNotificationEntityTask = CompanyCommunicatorSendFunction.sendingNotificationDataRepository.GetAsync(
                    PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                    messageContent.NotificationId);

                var getGlobalSendingNotificationDataEntityTask = CompanyCommunicatorSendFunction.globalSendingNotificationDataRepository
                    .GetGlobalSendingNotificationDataEntity();

                var incomingUserDataEntity = messageContent.UserDataEntity;
                var incomingConversationId = incomingUserDataEntity.ConversationId;

                var getUserDataEntityTask = string.IsNullOrWhiteSpace(incomingConversationId)
                    ? CompanyCommunicatorSendFunction.userDataRepository.GetAsync(
                        PartitionKeyNames.UserDataTable.UserDataPartition,
                        incomingUserDataEntity.AadId)
                    : Task.FromResult<UserDataEntity>(null);

                await Task.WhenAll(getActiveNotificationEntityTask, getGlobalSendingNotificationDataEntityTask, getUserDataEntityTask);

                var activeNotificationEntity = await getActiveNotificationEntityTask;
                var globalSendingNotificationDataEntity = await getGlobalSendingNotificationDataEntityTask;
                var userDataEntity = await getUserDataEntityTask;

                var conversationId = string.IsNullOrWhiteSpace(incomingConversationId)
                    ? userDataEntity?.ConversationId
                    : incomingConversationId;

                Task saveUserDataEntityTask = Task.CompletedTask;
                Task saveSentNotificationDataTask = Task.CompletedTask;
                Task setDelayTimeAndSendDelayedRetryTask = Task.CompletedTask;

                if (globalSendingNotificationDataEntity?.SendRetryDelayTime != null
                    && DateTime.UtcNow < globalSendingNotificationDataEntity.SendRetryDelayTime)
                {
                    await this.SendDelayedRetryOfMessageToSendFunction(CompanyCommunicatorSendFunction.configuration, messageContent);

                    return;
                }

                if (!string.IsNullOrWhiteSpace(conversationId))
                {
                    incomingUserDataEntity.ConversationId = conversationId;

                    // Check if message is intended for a team
                    if (!conversationId.StartsWith("19:"))
                    {
                        incomingUserDataEntity.PartitionKey = PartitionKeyNames.UserDataTable.UserDataPartition;
                        incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;

                        var operation = TableOperation.InsertOrMerge(incomingUserDataEntity);

                        saveUserDataEntityTask = CompanyCommunicatorSendFunction.userDataRepository.Table.ExecuteAsync(operation);
                    }
                }
                else
                {
                    var isCreateConversationThrottled = false;

                    for (int i = 0; i < maxNumberOfAttempts; i++)
                    {
                        var createConversationUrl = $"{incomingUserDataEntity.ServiceUrl}v3/conversations";
                        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, createConversationUrl))
                        {
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                                "Bearer",
                                CompanyCommunicatorSendFunction.botAccessToken);

                            var payloadString = "{\"bot\": { \"id\": \"28:" + CompanyCommunicatorSendFunction.configuration["MicrosoftAppId"] + "\"},\"isGroup\": false, \"tenantId\": \"" + incomingUserDataEntity.TenantId + "\", \"members\": [{\"id\": \"" + incomingUserDataEntity.UserId + "\"}]}";
                            requestMessage.Content = new StringContent(payloadString, Encoding.UTF8, "application/json");

                            using (var sendResponse = await CompanyCommunicatorSendFunction.httpClient.SendAsync(requestMessage))
                            {
                                if (sendResponse.StatusCode == HttpStatusCode.Created)
                                {
                                    var jsonResponseString = await sendResponse.Content.ReadAsStringAsync();
                                    dynamic resp = JsonConvert.DeserializeObject(jsonResponseString);

                                    incomingUserDataEntity.PartitionKey = PartitionKeyNames.UserDataTable.UserDataPartition;
                                    incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;
                                    incomingUserDataEntity.ConversationId = resp.id;

                                    var operation = TableOperation.InsertOrMerge(incomingUserDataEntity);

                                    saveUserDataEntityTask = CompanyCommunicatorSendFunction.userDataRepository.Table.ExecuteAsync(operation);

                                    isCreateConversationThrottled = false;

                                    break;
                                }
                                else if (sendResponse.StatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    isCreateConversationThrottled = true;

                                    totalNumberOfThrottles++;

                                    // Do not delay if already attempted the maximum number of attempts.
                                    if (i != maxNumberOfAttempts - 1)
                                    {
                                        var random = new Random();
                                        await Task.Delay(random.Next(500, 1500));
                                    }
                                }
                                else
                                {
                                    await this.SaveSentNotificationData(
                                        messageContent.NotificationId,
                                        incomingUserDataEntity.AadId,
                                        totalNumberOfThrottles,
                                        isStatusCodeFromCreateConversation: true,
                                        statusCode: sendResponse.StatusCode);

                                    return;
                                }
                            }
                        }
                    }

                    if (isCreateConversationThrottled)
                    {
                        await this.SetDelayTimeAndSendDelayedRetry(CompanyCommunicatorSendFunction.configuration, messageContent);

                        return;
                    }
                }

                var isSendMessageThrottled = false;

                for (int i = 0; i < maxNumberOfAttempts; i++)
                {
                    var conversationUrl = $"{incomingUserDataEntity.ServiceUrl}v3/conversations/{incomingUserDataEntity.ConversationId}/activities";
                    using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, conversationUrl))
                    {
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                            "Bearer",
                            CompanyCommunicatorSendFunction.botAccessToken);

                        var attachmentJsonString = JsonConvert.DeserializeObject(activeNotificationEntity.Content);
                        var messageString = "{ \"type\": \"message\", \"attachments\": [ { \"contentType\": \"application/vnd.microsoft.card.adaptive\", \"content\": " + attachmentJsonString + " } ] }";
                        requestMessage.Content = new StringContent(messageString, Encoding.UTF8, "application/json");

                        using (var sendResponse = await CompanyCommunicatorSendFunction.httpClient.SendAsync(requestMessage))
                        {
                            if (sendResponse.StatusCode == HttpStatusCode.Created)
                            {
                                log.LogInformation("MESSAGE SENT SUCCESSFULLY");

                                saveSentNotificationDataTask = this.SaveSentNotificationData(
                                    messageContent.NotificationId,
                                    incomingUserDataEntity.AadId,
                                    totalNumberOfThrottles,
                                    isStatusCodeFromCreateConversation: false,
                                    statusCode: sendResponse.StatusCode);

                                isSendMessageThrottled = false;

                                break;
                            }
                            else if (sendResponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                log.LogError("MESSAGE THROTTLED");

                                isSendMessageThrottled = true;

                                totalNumberOfThrottles++;

                                // Do not delay if already attempted the maximum number of attempts.
                                if (i != maxNumberOfAttempts - 1)
                                {
                                    var random = new Random();
                                    await Task.Delay(random.Next(500, 1500));
                                }
                            }
                            else
                            {
                                log.LogError($"MESSAGE FAILED: {sendResponse.StatusCode}");

                                saveSentNotificationDataTask = this.SaveSentNotificationData(
                                    messageContent.NotificationId,
                                    incomingUserDataEntity.AadId,
                                    totalNumberOfThrottles,
                                    isStatusCodeFromCreateConversation: false,
                                    statusCode: sendResponse.StatusCode);

                                await Task.WhenAll(saveUserDataEntityTask, saveSentNotificationDataTask);

                                return;
                            }
                        }
                    }
                }

                if (isSendMessageThrottled)
                {
                    setDelayTimeAndSendDelayedRetryTask =
                        this.SetDelayTimeAndSendDelayedRetry(CompanyCommunicatorSendFunction.configuration, messageContent);
                }

                await Task.WhenAll(
                    saveUserDataEntityTask,
                    saveSentNotificationDataTask,
                    setDelayTimeAndSendDelayedRetryTask);
            }
            catch (Exception e)
            {
                log.LogError(e, $"ERROR: {e.Message}, {e.GetType()}");

                var statusCodeToStore = HttpStatusCode.Continue;
                if (deliveryCount >= CompanyCommunicatorSendFunction.MaxDeliveryCountForDeadLetter)
                {
                    statusCodeToStore = HttpStatusCode.InternalServerError;
                }

                await this.SaveSentNotificationData(
                    messageContent.NotificationId,
                    messageContent.UserDataEntity.AadId,
                    totalNumberOfThrottles,
                    isStatusCodeFromCreateConversation: false,
                    statusCode: statusCodeToStore);

                throw e;
            }
        }

        private async Task SaveSentNotificationData(
            string notificationId,
            string aadId,
            int totalNumberOfThrottles,
            bool isStatusCodeFromCreateConversation,
            HttpStatusCode statusCode)
        {
            var updatedSentNotificationDataEntity = new SentNotificationDataEntity
            {
                PartitionKey = notificationId,
                RowKey = aadId,
                AadId = aadId,
                TotalNumberOfThrottles = totalNumberOfThrottles,
                SentDate = DateTime.UtcNow,
                IsStatusCodeFromCreateConversation = isStatusCodeFromCreateConversation,
                StatusCode = (int)statusCode,
            };

            if (statusCode == HttpStatusCode.Created)
            {
                updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Succeeded;
            }
            else if (statusCode == HttpStatusCode.TooManyRequests)
            {
                updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Throttled;
            }
            else
            {
                updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Failed;
            }

            var operation = TableOperation.InsertOrMerge(updatedSentNotificationDataEntity);

            await CompanyCommunicatorSendFunction.sentNotificationDataRepository.Table.ExecuteAsync(operation);
        }

        private async Task FetchTokenAsync(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            var values = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", configuration["MicrosoftAppId"] },
                    { "client_secret", configuration["MicrosoftAppPassword"] },
                    { "scope", "https://api.botframework.com/.default" },
                };
            var content = new FormUrlEncodedContent(values);

            using (var tokenResponse = await httpClient.PostAsync("https://login.microsoftonline.com/botframework.com/oauth2/v2.0/token", content))
            {
                if (tokenResponse.StatusCode == HttpStatusCode.OK)
                {
                    var accessTokenContent = await tokenResponse.Content.ReadAsAsync<AccessTokenResponse>();

                    CompanyCommunicatorSendFunction.botAccessToken = accessTokenContent.AccessToken;

                    var expiresInSeconds = 121;

                    // If parsing fails, out variable is set to 0, so need to set the default
                    if (!int.TryParse(accessTokenContent.ExpiresIn, out expiresInSeconds))
                    {
                        expiresInSeconds = 121;
                    }

                    // Remove two minutes in order to have a buffer amount of time.
                    CompanyCommunicatorSendFunction.botAccessTokenExpiration = DateTime.UtcNow + TimeSpan.FromSeconds(expiresInSeconds - 120);
                }
                else
                {
                    throw new Exception("Error fetching bot access token.");
                }
            }
        }

        private async Task SetDelayTimeAndSendDelayedRetry(
            IConfiguration configuration,
            ServiceBusSendQueueMessageContent queueMessageContent)
        {
            // Simply initialize the variable for certain build environments and versions
            var sendRetryDelayNumberOfMinutes = 0;

            // If parsing fails, out variable is set to 0, so need to set the default
            if (!int.TryParse(configuration["SendRetryDelayNumberOfMinutes"], out sendRetryDelayNumberOfMinutes))
            {
                sendRetryDelayNumberOfMinutes = 11;
            }

            // Shorten this time by 15 seconds to ensure that when the delayed retry message is taken off of the queue
            // the Send Retry Delay Time will be earlier and will not block it
            var sendRetryDelayTime = DateTime.UtcNow + TimeSpan.FromMinutes(sendRetryDelayNumberOfMinutes - 0.25);

            var globalSendingNotificationDataEntity = new GlobalSendingNotificationDataEntity
            {
                SendRetryDelayTime = sendRetryDelayTime,
            };

            await CompanyCommunicatorSendFunction.globalSendingNotificationDataRepository
                .SetGlobalSendingNotificationDataEntity(globalSendingNotificationDataEntity);

            await this.SendDelayedRetryOfMessageToSendFunction(configuration, queueMessageContent);
        }

        private async Task SendDelayedRetryOfMessageToSendFunction(
            IConfiguration configuration,
            ServiceBusSendQueueMessageContent queueMessageContent)
        {
            // Simply initialize the variable for certain build environments and versions
            var sendRetryDelayNumberOfMinutes = 0;

            // If parsing fails, out variable is set to 0, so need to set the default
            if (!int.TryParse(configuration["SendRetryDelayNumberOfMinutes"], out sendRetryDelayNumberOfMinutes))
            {
                sendRetryDelayNumberOfMinutes = 11;
            }

            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromMinutes(sendRetryDelayNumberOfMinutes);

            await CompanyCommunicatorSendFunction.sendQueueServiceBusMessageSender.SendAsync(serviceBusMessage);
        }

        private class ServiceBusSendQueueMessageContent
        {
            public string NotificationId { get; set; }

            // This can be a team.id
            public UserDataEntity UserDataEntity { get; set; }
        }

        private class AccessTokenResponse
        {
            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public string ExpiresIn { get; set; }

            [JsonProperty("ext_expires_in")]
            public string ExtExpiresIn { get; set; }

            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }
    }
}
