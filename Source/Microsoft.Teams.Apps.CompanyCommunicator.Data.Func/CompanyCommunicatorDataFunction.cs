// <copyright file="CompanyCommunicatorDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for incrementing results for a sent notification.
    /// </summary>
    public class CompanyCommunicatorDataFunction
    {
        private readonly IConfiguration configuration;
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorDataFunction"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        public CompanyCommunicatorDataFunction(
            IConfiguration configuration,
            NotificationDataRepository notificationDataRepository)
        {
            this.configuration = configuration;
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for aggregating results for a sent notification.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="deliveryCount">The deliver count.</param>
        /// <param name="enqueuedTimeUtc">The enqueued time.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="log">The logger.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorDataFunction")]
        public async Task Run(
            [ServiceBusTrigger(
                DataQueue.QueueName,
                Connection = DataQueue.ServiceBusConnectionConfigurationKey)]
            string myQueueItem,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log,
            ExecutionContext context)
        {
            var messageContent = JsonConvert.DeserializeObject<DataQueueMessageContent>(myQueueItem);

            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                partitionKey: PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                rowKey: messageContent.NotificationId);

            // This is true if it is the delayed service bus message that ensures that the
            // notification will eventually be marked as complete.
            if (messageContent.ForceMessageComplete)
            {
                // If the notification is already marked complete, then nothing needs to be done.
                if (!notificationDataEntity.IsCompleted)
                {
                    var incompleteTotalMessageCount = notificationDataEntity.Succeeded
                        + notificationDataEntity.Throttled
                        + notificationDataEntity.Failed;

                    var unknownCount = notificationDataEntity.TotalMessageCount - incompleteTotalMessageCount;

                    var forcedNotificationDataEntityUpdate = new UpdateNotificationDataEntity
                    {
                        PartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                        RowKey = messageContent.NotificationId,
                        Unknown = unknownCount,
                        IsCompleted = true,
                        SentDate = DateTime.UtcNow,
                    };

                    var forcedOperation = TableOperation.InsertOrMerge(forcedNotificationDataEntityUpdate);
                    await CompanyCommunicatorDataFunction.notificationDataRepository.Table.ExecuteAsync(forcedOperation);
                }

                return;
            }

            var succeededCount = notificationDataEntity.Succeeded;
            var throttledCount = notificationDataEntity.Throttled;
            var failedCount = notificationDataEntity.Failed;

            if (messageContent.ResultType == DataQueueResultType.Succeeded)
            {
                succeededCount++;
            }
            else if (messageContent.ResultType == DataQueueResultType.Throttled)
            {
                throttledCount++;
            }
            else
            {
                failedCount++;
            }

            // Purposefully exclude the unknown count because those messages may be sent later
            var currentTotalMessageCount = succeededCount
                + throttledCount
                + failedCount;

            var notificationDataEntityUpdate = new UpdateNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                RowKey = messageContent.NotificationId,
                Succeeded = succeededCount,
                Failed = failedCount,
                Throttled = throttledCount,
            };

            if (currentTotalMessageCount >= notificationDataEntity.TotalMessageCount)
            {
                notificationDataEntityUpdate.IsCompleted = true;
                notificationDataEntityUpdate.SentDate = messageContent.SentDate ?? DateTime.UtcNow;
            }

            var operation = TableOperation.InsertOrMerge(notificationDataEntityUpdate);
            await CompanyCommunicatorDataFunction.notificationDataRepository.Table.ExecuteAsync(operation);
        }

        ////CompanyCommunicatorDataFunction.notificationDataRepository = CompanyCommunicatorDataFunction.notificationDataRepository
        ////        ?? this.CreateNotificationRepository(CompanyCommunicatorDataFunction.configuration);

        ////private NotificationDataRepository CreateNotificationRepository(IConfiguration configuration)
        ////{
        ////    var tableRowKeyGenerator = new TableRowKeyGenerator();
        ////    return new NotificationDataRepository(configuration, tableRowKeyGenerator, isFromAzureFunction: true);
        ////}
    }
}
