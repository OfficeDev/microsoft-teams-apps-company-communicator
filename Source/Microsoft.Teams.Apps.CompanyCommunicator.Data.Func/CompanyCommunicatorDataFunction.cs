// <copyright file="CompanyCommunicatorDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for incrementing results for a sent notification.
    /// </summary>
    public class CompanyCommunicatorDataFunction
    {
        private static readonly double TenMinutes = 10;

        private readonly INotificationDataRepository notificationDataRepository;
        private readonly AggregateSentNotificationDataService aggregateSentNotificationDataService;
        private readonly UpdateNotificationDataService updateNotificationDataService;
        private readonly IDataQueue dataQueue;
        private readonly double firstTenMinutesRequeueMessageDelayInSeconds;
        private readonly double requeueMessageDelayInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorDataFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        /// <param name="aggregateSentNotificationDataService">The service to aggregate the Sent
        /// Notification Data results.</param>
        /// <param name="updateNotificationDataService">The service to update the notification totals.</param>
        /// <param name="dataQueue">The data queue.</param>
        /// <param name="dataQueueMessageOptions">The data queue message options.</param>
        public CompanyCommunicatorDataFunction(
            INotificationDataRepository notificationDataRepository,
            AggregateSentNotificationDataService aggregateSentNotificationDataService,
            UpdateNotificationDataService updateNotificationDataService,
            IDataQueue dataQueue,
            IOptions<DataQueueMessageOptions> dataQueueMessageOptions)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.aggregateSentNotificationDataService = aggregateSentNotificationDataService;
            this.updateNotificationDataService = updateNotificationDataService;
            this.dataQueue = dataQueue;
            this.firstTenMinutesRequeueMessageDelayInSeconds =
                dataQueueMessageOptions.Value.FirstTenMinutesRequeueMessageDelayInSeconds;
            this.requeueMessageDelayInSeconds =
                dataQueueMessageOptions.Value.RequeueMessageDelayInSeconds;
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
                partitionKey: NotificationDataTableNames.SentNotificationsPartition,
                rowKey: messageContent.NotificationId);

            // If notification is already marked complete, then there is nothing left to do for the data queue trigger.
            if (!notificationDataEntity.IsCompleted())
            {
                // Get all of the result counts (Successes, Failures, etc.) from the Sent Notification Data.
                var aggregatedSentNotificationDataResults = await this.aggregateSentNotificationDataService
                    .AggregateSentNotificationDataResultsAsync(messageContent.NotificationId, log);

                // Use these counts to update the Notification Data accordingly.
                var notificationDataEntityUpdate = await this.updateNotificationDataService
                    .UpdateNotificationDataAsync(
                        notificationId: messageContent.NotificationId,
                        shouldForceCompleteNotification: messageContent.ForceMessageComplete,
                        totalExpectedNotificationCount: notificationDataEntity.TotalMessageCount,
                        aggregatedSentNotificationDataResults: aggregatedSentNotificationDataResults,
                        log: log);

                // If the notification is still not in a completed state, then requeue the Data Queue trigger
                // message with a delay in order to aggregate the results again.
                if (!notificationDataEntityUpdate.IsCompleted())
                {
                    // Requeue data aggregation trigger message with a delay to calculate the totals again.
                    var dataQueueTriggerMessage = new DataQueueMessageContent
                    {
                        NotificationId = messageContent.NotificationId,
                        ForceMessageComplete = false,
                    };

                    var dataQueueTriggerMessageDelayInSeconds =
                        DateTime.UtcNow <= notificationDataEntity.SendingStartedDate + TimeSpan.FromMinutes(CompanyCommunicatorDataFunction.TenMinutes)
                            ? this.firstTenMinutesRequeueMessageDelayInSeconds
                            : this.requeueMessageDelayInSeconds;

                    await this.dataQueue.SendDelayedAsync(dataQueueTriggerMessage, dataQueueTriggerMessageDelayInSeconds);
                }
            }
        }
    }
}
