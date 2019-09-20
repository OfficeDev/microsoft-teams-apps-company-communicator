// <copyright file="CompanyCommunicatorPrepareToSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for preparing to send a notification.
    /// </summary>
    public class CompanyCommunicatorPrepareToSendFunction
    {
        private const string QueueName = "company-communicator-preparetosend";
        private const string ConnectionName = "ServiceBusConnection";
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorPrepareToSendFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository service.</param>
        public CompanyCommunicatorPrepareToSendFunction(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for kicking off the durable orchestration for preparing to send notifications.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="starter">Durable orchestration client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorPrepareToSendFunction")]
        public async Task Run(
            [ServiceBusTrigger(
                CompanyCommunicatorPrepareToSendFunction.QueueName,
                Connection = CompanyCommunicatorPrepareToSendFunction.ConnectionName)]
            string myQueueItem,
            [OrchestrationClient]
            DurableOrchestrationClient starter)
        {
            var partitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition;
            var notificationDataEntityId = JsonConvert.DeserializeObject<string>(myQueueItem);
            var notificationDataEntity =
                await this.notificationDataRepository.GetAsync(partitionKey, notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                string instanceId = await starter.StartNewAsync(
                    nameof(PreparingToSendOrchestration.PrepareToSendOrchestrationAsync),
                    notificationDataEntity);
            }
        }
    }
}
