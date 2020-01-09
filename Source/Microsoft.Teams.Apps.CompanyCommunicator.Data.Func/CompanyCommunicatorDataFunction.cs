// <copyright file="CompanyCommunicatorDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for incrementing results for a sent notification.
    /// </summary>
    public class CompanyCommunicatorDataFunction
    {
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly ForceCompleteNotificationDataService forceCompleteNotificationDataService;
        private readonly UpdateCountsInNotificationDataService updateCountsInNotificationDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorDataFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        /// <param name="forceCompleteNotificationDataService">The force complete notification data service.</param>
        /// <param name="updateCountsInNotificationDataService">The update counts in notification data service.</param>
        public CompanyCommunicatorDataFunction(
            NotificationDataRepository notificationDataRepository,
            ForceCompleteNotificationDataService forceCompleteNotificationDataService,
            UpdateCountsInNotificationDataService updateCountsInNotificationDataService)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.forceCompleteNotificationDataService = forceCompleteNotificationDataService;
            this.updateCountsInNotificationDataService = updateCountsInNotificationDataService;
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
                await this.forceCompleteNotificationDataService.ForceCompleteAsync(notificationDataEntity);

                return;
            }

            await this.updateCountsInNotificationDataService.UpdateCountsAsync(
                notificationDataEntity,
                messageContent.ResultType,
                messageContent.SentDate);
        }
    }
}
