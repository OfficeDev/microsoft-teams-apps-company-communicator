// <copyright file="ExportFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Orchestrator;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue.
    /// This function exports notification in a zip file for the admin.
    /// It prepares the file by reading the notification data, user graph api.
    /// This function stage the file in Blob Storage and send the
    /// file card to the admin using bot framework adapter.
    /// </summary>
    public class ExportFunction
    {
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly IExportDataRepository exportDataRepository;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="exportDataRepository">Export data repository.</param>
        /// <param name="localizer">Localization service.</param>
        public ExportFunction(
            INotificationDataRepository notificationDataRepository,
            IExportDataRepository exportDataRepository,
            IStringLocalizer<Strings> localizer)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.exportDataRepository = exportDataRepository ?? throw new ArgumentNullException(nameof(exportDataRepository));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue.
        /// It kicks off the durable orchestration for exporting notifications.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="starter">Durable orchestration client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorExportFunction")]
        public async Task Run(
            [ServiceBusTrigger(
             ExportQueue.QueueName,
             Connection = ExportQueue.ServiceBusConnectionConfigurationKey)]
            string myQueueItem,
            [DurableClient]
            IDurableOrchestrationClient starter)
        {
            if (myQueueItem == null)
            {
                throw new ArgumentNullException(nameof(myQueueItem));
            }

            if (starter == null)
            {
                throw new ArgumentNullException(nameof(starter));
            }

            var messageContent = JsonConvert.DeserializeObject<ExportMessageQueueContent>(myQueueItem);
            var notificationId = messageContent.NotificationId;

            var sentNotificationDataEntity = await this.notificationDataRepository.GetAsync(
                partitionKey: NotificationDataTableNames.SentNotificationsPartition,
                rowKey: notificationId);
            var exportDataEntity = await this.exportDataRepository.GetAsync(messageContent.UserId, notificationId);
            exportDataEntity.FileName = this.GetFileName();
            var requirement = new ExportDataRequirement(sentNotificationDataEntity, exportDataEntity, messageContent.UserId);
            if (requirement.IsValid())
            {
                string instanceId = await starter.StartNewAsync(
                    nameof(ExportOrchestration.ExportOrchestrationAsync),
                    requirement);
            }
        }

        private string GetFileName()
        {
            var guid = Guid.NewGuid().ToString();
            var fileName = this.localizer.GetString("FileName_ExportData");
            return $"{fileName}_{guid}.zip";
        }
    }
}
