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
    /// Azure Function App triggered by messages from a Service Bus queue.
    /// It prepares to send a notification to target recipients.
    /// </summary>
    public class CompanyCommunicatorPrepareToSendFunction
    {
        private const string QueueName = "company-communicator-preparetosend";
        private const string ConnectionName = "ServiceBusConnection";
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorPrepareToSendFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory service.</param>
        public CompanyCommunicatorPrepareToSendFunction(
            NotificationDataRepositoryFactory notificationDataRepositoryFactory)
        {
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// It kicks off the durable orchestration for preparing to send notifications.
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
            var notificationDataRepository = this.notificationDataRepositoryFactory.CreateRepository(true);
            var notificationDataEntity = await notificationDataRepository.GetAsync(partitionKey, notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                string instanceId = await starter.StartNewAsync(
                    nameof(PreparingToSendOrchestration.PrepareToSendOrchestrationAsync),
                    notificationDataEntity);
            }
        }
    }
}
