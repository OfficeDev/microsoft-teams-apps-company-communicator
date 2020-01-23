// <copyright file="CompanyCommunicatorSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.AccessTokenServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.DataServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for sending messages from the bot.
    /// </summary>
    public class CompanyCommunicatorSendFunction
    {
        /// <summary>
        /// This is set to 10 because the default maximum delivery count from the service bus
        /// message queue before the service bus will automatically put the message in the Dead Letter
        /// Queue is 10.
        /// </summary>
        private static readonly int MaxDeliveryCountForDeadLetter = 10;

        // Set as static so all instances can share the same access token.
        private static string botAccessToken = null;
        private static DateTime? botAccessTokenExpiration = null;

        private readonly int maxNumberOfAttempts;
        private readonly int sendRetryDelayNumberOfMinutes;
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository;
        private readonly UserDataRepository userDataRepository;
        private readonly SendQueue sendQueue;
        private readonly GetBotAccessTokenService getBotAccessTokenService;
        private readonly CreateUserConversationService createUserConversationService;
        private readonly SendNotificationService sendNotificationService;
        private readonly DelaySendingNotificationService delaySendingNotificationService;
        private readonly ManageResultDataService manageResultDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorSendFunction"/> class.
        /// </summary>
        /// <param name="companyCommunicatorSendFunctionOptions">The Company Communicator send function options.</param>
        /// <param name="sendingNotificationDataRepository">The sending notification data repository.</param>
        /// <param name="globalSendingNotificationDataRepository">The global sending notification data repository.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="sendQueue">The send queue.</param>
        /// <param name="getBotAccessTokenService">The get bot access token service.</param>
        /// <param name="createUserConversationService">The create user conversation service.</param>
        /// <param name="sendNotificationService">The send notification service.</param>
        /// <param name="delaySendingNotificationService">The delay sending notification service.</param>
        /// <param name="manageResultDataService">The manage result data service.</param>
        public CompanyCommunicatorSendFunction(
            IOptions<CompanyCommunicatorSendFunctionOptions> companyCommunicatorSendFunctionOptions,
            SendingNotificationDataRepository sendingNotificationDataRepository,
            GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository,
            UserDataRepository userDataRepository,
            SendQueue sendQueue,
            GetBotAccessTokenService getBotAccessTokenService,
            CreateUserConversationService createUserConversationService,
            SendNotificationService sendNotificationService,
            DelaySendingNotificationService delaySendingNotificationService,
            ManageResultDataService manageResultDataService)
        {
            this.maxNumberOfAttempts = companyCommunicatorSendFunctionOptions.Value.MaxNumberOfAttempts;
            this.sendRetryDelayNumberOfMinutes = companyCommunicatorSendFunctionOptions.Value.SendRetryDelayNumberOfMinutes;
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.globalSendingNotificationDataRepository = globalSendingNotificationDataRepository;
            this.userDataRepository = userDataRepository;
            this.sendQueue = sendQueue;
            this.getBotAccessTokenService = getBotAccessTokenService;
            this.createUserConversationService = createUserConversationService;
            this.sendNotificationService = sendNotificationService;
            this.delaySendingNotificationService = delaySendingNotificationService;
            this.manageResultDataService = manageResultDataService;
        }

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
            [ServiceBusTrigger(
                SendQueue.QueueName,
                Connection = SendQueue.ServiceBusConnectionConfigurationKey)]
            string myQueueItem,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            var messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(myQueueItem);

            var totalNumberOfThrottles = 0;

            try
            {
                // Check the shared access token. If it is not present or is invalid, then fetch a new one.
                if (CompanyCommunicatorSendFunction.botAccessToken == null
                    || CompanyCommunicatorSendFunction.botAccessTokenExpiration == null
                    || DateTime.UtcNow > CompanyCommunicatorSendFunction.botAccessTokenExpiration)
                {
                    var botAccessTokenServiceResponse = await this.getBotAccessTokenService.GetTokenAsync();
                    CompanyCommunicatorSendFunction.botAccessToken = botAccessTokenServiceResponse.BotAccessToken;
                    CompanyCommunicatorSendFunction.botAccessTokenExpiration = botAccessTokenServiceResponse.BotAccessTokenExpiration;
                }

                // Fetch the current sending notification. This is where data about what is being sent is stored.
                var getActiveNotificationEntityTask = this.sendingNotificationDataRepository.GetAsync(
                    PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                    messageContent.NotificationId);

                // Fetch the current global sending notification data. This is where data about the overall systems
                // status is stored e.g. is everything in a delayed state because the bot is being throttled.
                var getGlobalSendingNotificationDataEntityTask = this.globalSendingNotificationDataRepository
                    .GetGlobalSendingNotificationDataEntityAsync();

                var incomingUserDataEntity = messageContent.UserDataEntity;
                var incomingConversationId = incomingUserDataEntity.ConversationId;

                // If the incoming payload does not have a conversationId, fetch the data for that user.
                var getUserDataEntityTask = string.IsNullOrWhiteSpace(incomingConversationId)
                    ? this.userDataRepository.GetAsync(
                        PartitionKeyNames.UserDataTable.UserDataPartition,
                        incomingUserDataEntity.AadId)
                    : Task.FromResult<UserDataEntity>(null);

                await Task.WhenAll(getActiveNotificationEntityTask, getGlobalSendingNotificationDataEntityTask, getUserDataEntityTask);

                var activeNotificationEntity = await getActiveNotificationEntityTask;
                var globalSendingNotificationDataEntity = await getGlobalSendingNotificationDataEntityTask;
                var userDataEntity = await getUserDataEntityTask;

                // If the incoming conversationId was not present, attempt to use the conversationId stored for
                // that user.
                // NOTE: It is possible that that user's data has not been stored in the user data repository.
                // If this is the case, then the conversation will have to be created for that user.
                var conversationId = string.IsNullOrWhiteSpace(incomingConversationId)
                    ? userDataEntity?.ConversationId
                    : incomingConversationId;

                // Initiate tasks that will be run in parallel if the step is required.
                var saveUserDataEntityTask = Task.CompletedTask;
                var proccessResultDataTask = Task.CompletedTask;
                var delaySendingNotificationTask = Task.CompletedTask;

                // If the overall system is in a throttled state and needs to be delayed,
                // add the message back on the queue with a delay.
                if (globalSendingNotificationDataEntity?.SendRetryDelayTime != null
                    && DateTime.UtcNow < globalSendingNotificationDataEntity.SendRetryDelayTime)
                {
                    await this.sendQueue.SendDelayedAsync(messageContent, this.sendRetryDelayNumberOfMinutes);

                    return;
                }

                // If the conversationId is known, the conversation does not need to be created.
                // If it a conversationId for a team, then nothing more needs to be done.
                // If it is a conversationId for a user, it is possible that the incoming user data has
                // more information than what is currently stored in the user data repository. Because of this,
                // save/update that user's information.
                if (!string.IsNullOrWhiteSpace(conversationId))
                {
                    // Set the conversationId so it is not removed from the user data repository on the update.
                    incomingUserDataEntity.ConversationId = conversationId;

                    // Verify that the conversationId is for a user (starting with 19: means it is for a team's
                    // General channel).
                    if (!conversationId.StartsWith("19:"))
                    {
                        incomingUserDataEntity.PartitionKey = PartitionKeyNames.UserDataTable.UserDataPartition;
                        incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;

                        // It is possible that the incoming user data has more information than what is currently
                        // stored in the user data repository, so save/update that user's information.
                        saveUserDataEntityTask = this.userDataRepository.InsertOrMergeAsync(incomingUserDataEntity);
                    }
                }
                else
                {
                    /*
                     * Falling into this block means that the message is meant for a user, but a conversationId
                     * is not known for that user (most likely "send to a team's members" option was selected
                     * as the audience). Because of this, the conversation needs to be created and that
                     * conversationId needs to be stored for that user.
                     */

                    var createConversationResponse = await this.createUserConversationService.CreateConversationAsync(
                        incomingUserDataEntity,
                        CompanyCommunicatorSendFunction.botAccessToken,
                        this.maxNumberOfAttempts);

                    totalNumberOfThrottles += createConversationResponse.NumberOfThrottleResponses;

                    if (createConversationResponse.ResultType == CreateUserConversationResultType.Succeeded)
                    {
                        conversationId = createConversationResponse.ConversationId;

                        incomingUserDataEntity.PartitionKey = PartitionKeyNames.UserDataTable.UserDataPartition;
                        incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;
                        incomingUserDataEntity.ConversationId = conversationId;

                        saveUserDataEntityTask = this.userDataRepository.InsertOrMergeAsync(incomingUserDataEntity);
                    }
                    else if (createConversationResponse.ResultType == CreateUserConversationResultType.Throttled)
                    {
                        // If the request was attempted the maximum number of attempts and received
                        // all throttling responses, then set the overall delay time for the system so all
                        // other calls will be delayed and add the message back to the queue with a delay to be
                        // attempted later.
                        await this.delaySendingNotificationService.DelaySendingNotificationAsync(
                            this.sendRetryDelayNumberOfMinutes,
                            messageContent);

                        return;
                    }
                    else if (createConversationResponse.ResultType == CreateUserConversationResultType.Failed)
                    {
                        // If the create conversation call failed, save the result, do not attempt the
                        // request again, and end the function.
                        await this.manageResultDataService.ProccessResultDataAsync(
                            messageContent.NotificationId,
                            incomingUserDataEntity.AadId,
                            totalNumberOfThrottles,
                            isStatusCodeFromCreateConversation: true,
                            statusCode: createConversationResponse.StatusCode);

                        return;
                    }
                }

                // Now that all of the necessary information is known, send the notification.
                var sendNotificationResponse = await this.sendNotificationService.SendAsync(
                    activeNotificationEntity.Content,
                    incomingUserDataEntity.ServiceUrl,
                    conversationId,
                    CompanyCommunicatorSendFunction.botAccessToken,
                    this.maxNumberOfAttempts);

                totalNumberOfThrottles += sendNotificationResponse.NumberOfThrottleResponses;

                if (sendNotificationResponse.ResultType == SendNotificationResultType.Succeeded)
                {
                    log.LogInformation("MESSAGE SENT SUCCESSFULLY");

                    proccessResultDataTask = this.manageResultDataService.ProccessResultDataAsync(
                        messageContent.NotificationId,
                        incomingUserDataEntity.AadId,
                        totalNumberOfThrottles,
                        isStatusCodeFromCreateConversation: false,
                        statusCode: sendNotificationResponse.StatusCode);
                }
                else if (sendNotificationResponse.ResultType == SendNotificationResultType.Throttled)
                {
                    // If the request was attempted the maximum number of attempts and received
                    // all throttling responses, then set the overall delay time for the system so all
                    // other calls will be delayed and add the message back to the queue with a delay to be
                    // attempted later.
                    log.LogError("MESSAGE THROTTLED");

                    // NOTE: Here it does not immediately await this task and exit the function because a task
                    // of saving updated user data with a newly created conversation ID may need to be awaited.
                    delaySendingNotificationTask = this.delaySendingNotificationService
                        .DelaySendingNotificationAsync(this.sendRetryDelayNumberOfMinutes, messageContent);
                }
                else if (sendNotificationResponse.ResultType == SendNotificationResultType.Failed)
                {
                    // If in this block, then an error has occurred with the service.
                    // Save the relevant information and do not attempt the request again.
                    log.LogError($"MESSAGE FAILED: {sendNotificationResponse.StatusCode}");

                    // NOTE: Here it does not immediately await this task and exit the function because a task
                    // of saving updated user data with a newly created conversation ID may need to be awaited.
                    proccessResultDataTask = this.manageResultDataService.ProccessResultDataAsync(
                        messageContent.NotificationId,
                        incomingUserDataEntity.AadId,
                        totalNumberOfThrottles,
                        isStatusCodeFromCreateConversation: false,
                        statusCode: sendNotificationResponse.StatusCode);
                }

                await Task.WhenAll(
                    saveUserDataEntityTask,
                    proccessResultDataTask,
                    delaySendingNotificationTask);
            }
            catch (Exception e)
            {
                /*
                 * If in this block, then an exception was thrown. If the function throws an exception
                 * then the service bus message will be placed back on the queue. If this process has
                 * been done enough times and the message has been attempted to be delivered more than
                 * its allowed delivery count, then the message is placed on the dead letter queue of
                 * the service bus. For each attempt that did not result with the message being placed
                 * on the dead letter queue, set the status to be stored as HttpStatusCode.Continue. If
                 * the maximum delivery count has been reached and the message will be place on the
                 * dead letter queue, then set the status to be stored as HttpStatusCode.InternalServerError.
                 */

                log.LogError(e, $"ERROR: {e.Message}, {e.GetType()}");

                var statusCodeToStore = HttpStatusCode.Continue;
                if (deliveryCount >= CompanyCommunicatorSendFunction.MaxDeliveryCountForDeadLetter)
                {
                    statusCodeToStore = HttpStatusCode.InternalServerError;
                }

                await this.manageResultDataService.ProccessResultDataAsync(
                    messageContent.NotificationId,
                    messageContent.UserDataEntity.AadId,
                    totalNumberOfThrottles,
                    isStatusCodeFromCreateConversation: false,
                    statusCode: statusCodeToStore);

                throw e;
            }
        }
    }
}
