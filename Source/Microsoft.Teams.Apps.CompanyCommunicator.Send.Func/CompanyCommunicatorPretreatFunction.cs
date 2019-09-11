// <copyright file="CompanyCommunicatorPretreatFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for aggregating results for a sent notification.
    /// </summary>
    public class CompanyCommunicatorPretreatFunction
    {
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly NotificationDelivery.NotificationDelivery notificationDelivery;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorPretreatFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository service that deals with the table storage in azure.</param>
        /// <param name="notificationDelivery">Notification delivery service instance.</param>
        public CompanyCommunicatorPretreatFunction(
            NotificationDataRepository notificationDataRepository,
            NotificationDelivery.NotificationDelivery notificationDelivery)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.notificationDelivery = notificationDelivery;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for kicking off the sending notificaiton flow.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorPretreatFunction")]
        public async Task Run(
            [ServiceBusTrigger("company-communicator-pretreat", Connection = "ServiceBusConnection")]
            string myQueueItem,
            ILogger log)
        {
            var draftNotificationId = JsonConvert.DeserializeObject<string>(myQueueItem);
            var draftNotificationEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.DraftNotificationsPartition,
                draftNotificationId);
            if (draftNotificationEntity != null)
            {
                await this.notificationDelivery.SendAsync(draftNotificationEntity, log);
            }
        }
    }
}
