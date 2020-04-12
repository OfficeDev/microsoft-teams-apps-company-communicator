// <copyright file="CompanyCommunicatorPrepareToSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue.
    /// It prepares to send a notification to target recipients.
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
            [OrchestrationClient]
            DurableOrchestrationClient starter)
        {
            var sentNotificationsPartitionKey = NotificationDataTableNames.SentNotificationsPartition;
            var queueMessageContent = JsonConvert.DeserializeObject<PrepareToSendQueueMessageContent>(myQueueItem);
            var sentNotificationId = queueMessageContent.SentNotificationId;

            var sentNotificationDataEntity = await this.notificationDataRepository.GetAsync(sentNotificationsPartitionKey, sentNotificationId);
            if (sentNotificationDataEntity != null)
            {
                string instanceId = await starter.StartNewAsync(
                    nameof(PreparingToSendOrchestration.PrepareToSendOrchestrationAsync),
                    sentNotificationDataEntity);
            }
        }
    }
}
