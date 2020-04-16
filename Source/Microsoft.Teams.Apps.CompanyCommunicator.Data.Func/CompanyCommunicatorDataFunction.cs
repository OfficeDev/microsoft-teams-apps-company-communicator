// <copyright file="CompanyCommunicatorDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
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
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly AggregateSentNotificationDataService aggregateSentNotificationDataService;
        private readonly UpdateNotificationDataService updateNotificationDataService;
        private readonly DataQueue dataQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorDataFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        /// <param name="aggregateSentNotificationDataService">The service to aggregate the Sent
        /// Notification Data results.</param>
        /// <param name="updateNotificationDataService">The service to update the notification totals.</param>
        /// <param name="dataQueue">The data queue.</param>
        public CompanyCommunicatorDataFunction(
            NotificationDataRepository notificationDataRepository,
            AggregateSentNotificationDataService aggregateSentNotificationDataService,
            UpdateNotificationDataService updateNotificationDataService,
            DataQueue dataQueue)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.aggregateSentNotificationDataService = aggregateSentNotificationDataService;
            this.updateNotificationDataService = updateNotificationDataService;
            this.dataQueue = dataQueue;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for aggregating results for a sent notification.
        /// </summary>
        /// <param name="message">The Service Bus message.</param>
        /// <param name="messageReceiver">The Service Bus message receiver.</param>
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
            Message message,
            MessageReceiver messageReceiver,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log,
            ExecutionContext context)
        {
            //// NOTE: BECAUSE THIS AZURE FUNCTION IS MARKED TO NOT AUTOMATICALLY COMPLETE THE SERVICE BUS MESSAGE
            //// ALL CODE PATHS MUST END WITH COMPLETING THE SERVICE BUS MESSAGE MANUALLY

            var messageContent = JsonConvert.DeserializeObject<DataQueueMessageContent>(Encoding.UTF8.GetString(message.Body));

            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                partitionKey: NotificationDataTableNames.SentNotificationsPartition,
                rowKey: messageContent.NotificationId);

            // If notification is already marked complete, then there is nothing left to do for the data queue trigger.
            if (!notificationDataEntity.IsCompleted)
            {
                // Get all of the result counts (Successes, Failures, etc.) from the Sent Notification Data.
                var aggregatedSentNotificationDataResults = await this.aggregateSentNotificationDataService
                    .AggregateSentNotificationDataResultsAsync(
                        messageReceiver,
                        message,
                        messageContent.NotificationId);

                // Use these counts to update the Notification Data accordingly.
                var notificationDataEntityUpdate = await this.updateNotificationDataService
                    .UpdateNotificationDataAsync(
                        notificationId: messageContent.NotificationId,
                        shouldForceCompleteNotification: messageContent.ForceMessageComplete,
                        totalExpectedNotificationCount: notificationDataEntity.TotalMessageCount,
                        aggregatedSentNotificationDataResults: aggregatedSentNotificationDataResults);

                // If the notification is still not in a completed state, then requeue the Data Queue trigger
                // message with a delay in order to aggregate the results again.
                if (!notificationDataEntityUpdate.IsCompleted)
                {
                    // Requeue data aggregation trigger message with a delay to calculate the totals again.
                    var dataQueueTriggerMessage = new DataQueueMessageContent
                    {
                        NotificationId = messageContent.NotificationId,
                        SentDate = DateTime.UtcNow,
                        ResultType = DataQueueResultType.Succeeded,
                        ForceMessageComplete = false,
                    };

                    await this.dataQueue.SendDelayedAsync(dataQueueTriggerMessage, 3);
                }
            }

            // Be sure to complete the Service Bus message manually so it is removed from the queue.
            await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
        }
    }
}
