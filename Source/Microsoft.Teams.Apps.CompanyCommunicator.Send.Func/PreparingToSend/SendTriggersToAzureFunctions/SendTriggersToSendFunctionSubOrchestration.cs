// <copyright file="SendTriggersToSendFunctionSubOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// This class contains the following durable components:
    /// 1). The durable sub-orchestration ProcessRecipientBatchSubOrchestration.
    /// 2). And two durable activities, SendTriggersToSendFunctionAsync and SetRecipientBatchSatusAsync.
    /// The components work together to send triggers to the Azure send function.
    /// </summary>
    public class SendTriggersToSendFunctionSubOrchestration
    {
        private readonly SendQueue sendMessageQueue;
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;
        private readonly SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggersToSendFunctionSubOrchestration"/> class.
        /// </summary>
        /// <param name="sendMessageQueue">Send message queue service.</param>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory.</param>
        /// <param name="sentNotificationDataRepositoryFactory">Sent notification data repository service.</param>
        public SendTriggersToSendFunctionSubOrchestration(
            SendQueue sendMessageQueue,
            NotificationDataRepositoryFactory notificationDataRepositoryFactory,
            SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory)
        {
            this.sendMessageQueue = sendMessageQueue;
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
            this.sentNotificationDataRepositoryFactory = sentNotificationDataRepositoryFactory;
        }

        /// <summary>
        /// Run the sub-orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">New sent notification id.</param>
        /// <param name="recipientDataBatch">A recipient data batch.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataBatch)
        {
            var recipientStatusDictionary =
                this.GetRecipientStatusDictionary(notificationDataEntityId);

            await context.CallSubOrchestratorAsync(
                nameof(SendTriggersToSendFunctionSubOrchestration.SendTriggersToSendFunctionAsync),
                new SendTriggersToSendFunctionActivityDTO
                {
                    NotificationDataEntityId = notificationDataEntityId,
                    RecipientDataBatch = recipientDataBatch,
                    RecipientStatusDictionary = recipientStatusDictionary,
                });
        }

        /// <summary>
        /// This method represents a durable sub-orchestration that processes a recipient batch as follows.
        /// 1). Send triggers for recipients with "send notification status" equal to 0.
        /// 2). Set recipients' "send notification status" to 1 after queued triggers for them.
        /// The above two steps need to be executed as an unit in Fan-out / Fan-in pattern.
        /// That is why they are grouped in a sub-orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [FunctionName(nameof(SendTriggersToSendFunctionAsync))]
        public async Task SendTriggersToSendFunctionAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var input = context.GetInput<SendTriggersToSendFunctionActivityDTO>();

            try
            {
                await context.CallActivityWithRetryAsync(
                    nameof(SendTriggersToSendFunctionSubOrchestration.SendTriggersToAzureServiceBusAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
                    input);

                await context.CallActivityWithRetryAsync(
                    nameof(SendTriggersToSendFunctionSubOrchestration.SetRecipientBatchSatusAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
                    input);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.notificationDataRepositoryFactory.CreateRepository(true)
                    .SaveWarningInNotificationDataEntityAsync(input.NotificationDataEntityId, ex.Message);
            }
        }

        /// <summary>
        /// This method represents the  "send triggers to Azure service bus" activity.
        /// It sends trigger to the Azure send function.
        /// It sends trigger for recipients whose status is 0 only.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggersToAzureServiceBusAsync))]
        public async Task SendTriggersToAzureServiceBusAsync(
            [ActivityTrigger] SendTriggersToSendFunctionActivityDTO input)
        {
            var recipientDataBatch = input.RecipientDataBatch;
            var notificationDataEntityId = input.NotificationDataEntityId;
            var recipientStatusDictionary = input.RecipientStatusDictionary;

            var messages = recipientDataBatch
                .Select(recipientData =>
                    this.CreateServiceBusQueueMessage(
                        recipientData,
                        notificationDataEntityId,
                        recipientStatusDictionary))
                .Where(message => message != null)
                .ToList();

            await this.sendMessageQueue.SendAsync(messages);
        }

        /// <summary>
        /// This method represents the "set recipient batch status" durable activity.
        /// It sets recipients' status to 1 after sending triggers to the send queue.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SetRecipientBatchSatusAsync))]
        public async Task SetRecipientBatchSatusAsync(
            [ActivityTrigger] SendTriggersToSendFunctionActivityDTO input)
        {
            var notificationDataEntityId = input.NotificationDataEntityId;
            var recipientDataBatch = input.RecipientDataBatch;

            await this.SetStatusInSentNotificationDataAsync(
                notificationDataEntityId,
                recipientDataBatch);
        }

        /// <summary>
        /// Set "sent notification data status" to be 1 for recipients.
        /// It marks that messages are already queued for the recipients in Azure service bus.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private async Task SetStatusInSentNotificationDataAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataBatch)
        {
            // Retrieve AadIds whose StatusCode equal to 0 (in SentNotificationDataRepository).
            var filter =
                TableQuery.GenerateFilterCondition(nameof(SentNotificationDataEntity.StatusCode), QueryComparisons.Equal, "0");
            var filteredSentNotificationDataList =
                await this.sentNotificationDataRepositoryFactory.CreateRepository(false).GetWithFilterAsync(filter, notificationDataEntityId);
            var aadIdList = filteredSentNotificationDataList.Select(p => p.AadId);
            var aadIdHashSet = new HashSet<string>(aadIdList);

            // Set the StatusCode to be 1 for the above AadIds (in SentNotificationDataRepository).
            var sentNotificationDataEntities = recipientDataBatch
                .Where(p => aadIdHashSet.Contains(p.AadId))
                .Select(p =>
                    new SentNotificationDataEntity
                    {
                        PartitionKey = notificationDataEntityId,
                        RowKey = p.AadId,
                        AadId = p.AadId,
                        StatusCode = 1,
                    })
                .ToList();
            await this.sentNotificationDataRepositoryFactory.CreateRepository(true).BatchInsertOrMergeAsync(sentNotificationDataEntities);
        }

        private Message CreateServiceBusQueueMessage(
            UserDataEntity recipientData,
            string notificationDataEntityId,
            IDictionary<string, int> recipientStatusDictionary)
        {
            if (recipientStatusDictionary.TryGetValue(recipientData.AadId, out int status))
            {
                if (status == 0)
                {
                    var queueMessageContent = new SendQueueMessageContent
                    {
                        NotificationId = notificationDataEntityId,
                        UserDataEntity = recipientData,
                    };
                    var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                    message.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                    return message;
                }
            }

            return null;
        }

        private IDictionary<string, int> GetRecipientStatusDictionary(string notificationDataEntityId)
        {
            var recipientStatusDictionary = new Dictionary<string, int>();

            var sentNotificationDataEntityList =
                this.sentNotificationDataRepositoryFactory.CreateRepository(true).GetAllAsync(notificationDataEntityId).Result;

            foreach (var sentNotificationDataEntity in sentNotificationDataEntityList)
            {
                recipientStatusDictionary.Add(
                    sentNotificationDataEntity.RowKey,
                    sentNotificationDataEntity.StatusCode);
            }

            return recipientStatusDictionary;
        }
    }
}
