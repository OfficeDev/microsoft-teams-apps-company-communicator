// <copyright file="CompanyCommunicatorPrepareToSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue.
    /// This function prepares to send a notification to the target audience.
    /// It prepares the notification for sending by reading the notification data,
    /// creating initialization rows in the SentNotification data table for each recipient
    /// to later hold the results of sending a notification to that recipient, fetching the
    /// parameters for creating the notification's payload, creating and storing the notification's payload,
    /// sending a data aggregation trigger to the data queue, and sending a queue message to the
    /// send queue for each recipient.
    /// </summary>
    public class CompanyCommunicatorPrepareToSendFunction
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorPrepareToSendFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        public CompanyCommunicatorPrepareToSendFunction(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue.
        /// It kicks off the durable orchestration for preparing to send notifications.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="starter">Durable orchestration client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorPrepareToSendFunction")]
        public async Task Run(
            [ServiceBusTrigger(
                PrepareToSendQueue.QueueName,
                Connection = PrepareToSendQueue.ServiceBusConnectionConfigurationKey)]
            string myQueueItem,
            [DurableClient]
            IDurableOrchestrationClient starter)
        {
            var queueMessageContent = JsonConvert.DeserializeObject<PrepareToSendQueueMessageContent>(myQueueItem);
            var notificationId = queueMessageContent.NotificationId;

            var sentNotificationDataEntity = await this.notificationDataRepository.GetAsync(
                partitionKey: NotificationDataTableNames.SentNotificationsPartition,
                rowKey: notificationId);
            if (sentNotificationDataEntity != null)
            {
                string instanceId = await starter.StartNewAsync(
                    nameof(PreparingToSendOrchestration.PrepareToSendOrchestrationAsync),
                    sentNotificationDataEntity);
            }
        }
    }
}
