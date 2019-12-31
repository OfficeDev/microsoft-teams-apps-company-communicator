// <copyright file="CompanyCommunicatorPrepareToSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue.
    /// It prepares to send a notification to target recipients.
    /// </summary>
    public class CompanyCommunicatorPrepareToSendFunction
    {
        private const string QueueName = "company-communicator-preparetosend";
        private const string ConnectionName = "ServiceBusConnection";
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
        /// <param name="message">The incoming message.</param>
        /// <param name="messageReceiver">The incoming message's receiver.</param>
        /// <param name="starter">Durable orchestration client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorPrepareToSendFunction")]
        public async Task Run(
            [ServiceBusTrigger(
                CompanyCommunicatorPrepareToSendFunction.QueueName,
                Connection = CompanyCommunicatorPrepareToSendFunction.ConnectionName)]
            string myQueueItem,
            Message message,
            MessageReceiver messageReceiver,
            [OrchestrationClient]
            DurableOrchestrationClient starter)
        {
            var sentNotificationsPartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition;
            var queueMessageContent = JsonConvert.DeserializeObject<PrepareToSendQueueMessageContent>(myQueueItem);
            var sentNotificationId = queueMessageContent.SentNotificationId;

            // Automatically complete the service bus message so that, if this notification takes a very long
            // time to prepare, the message will not time out on the queue and have the service bus resend
            // the message.
            await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);

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
