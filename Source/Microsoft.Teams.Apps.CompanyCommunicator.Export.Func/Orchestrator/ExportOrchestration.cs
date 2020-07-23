// <copyright file="ExportOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Orchestrator
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Activities;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Model;

    /// <summary>
    /// This class is the durable framework orchestration for exporting notifications.
    /// </summary>
    public class ExportOrchestration
    {
        private readonly ExportDataRepository exportDataRepository;
        private readonly UploadActivity uploadActivity;
        private readonly SendFileCardActivity sendFileCardActivity;
        private readonly GetMetaDataActivity getMetaDataActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportOrchestration"/> class.
        /// </summary>
        /// <param name="exportDataRepository">the export data repository.</param>
        /// <param name="uploadActivity">upload zip activity.</param>
        /// <param name="sendFileCardActivity">send file card activity.</param>
        /// <param name="getMetaDataActivity">get the metadata activity.</param>
        public ExportOrchestration(
            ExportDataRepository exportDataRepository,
            UploadActivity uploadActivity,
            SendFileCardActivity sendFileCardActivity,
            GetMetaDataActivity getMetaDataActivity)
        {
            this.exportDataRepository = exportDataRepository;
            this.uploadActivity = uploadActivity;
            this.sendFileCardActivity = sendFileCardActivity;
            this.getMetaDataActivity = getMetaDataActivity;
        }

        /// <summary>
        /// This is the durable orchestration method,
        /// which starts the export process.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(ExportOrchestrationAsync))]
        public async Task ExportOrchestrationAsync(
          [OrchestrationTrigger] IDurableOrchestrationContext context,
          ILogger log)
        {
            var exportRequiredData = context.GetInput<ExportDataRequirement>();
            var sentNotificationDataEntity = exportRequiredData.NotificationDataEntity;
            var exportDataEntity = exportRequiredData.ExportDataEntity;
            var metaData = await this.getMetaDataActivity.RunAsync(context, (sentNotificationDataEntity, exportDataEntity), log);
            await this.uploadActivity.RunAsync(context, (sentNotificationDataEntity, metaData, exportRequiredData.FileName), log);
            var sendResponse = await this.sendFileCardActivity.RunAsync(context, (exportRequiredData.UserId, exportRequiredData.NotificationDataEntity.Id, exportRequiredData.FileName), log);
            exportDataEntity.FileConsentId = sendResponse.ResponseId;
            exportDataEntity.FileName = exportRequiredData.FileName;

            await context.CallActivityWithRetryAsync<Task>(
                   nameof(ExportOrchestration.UpdateExportDataActivityAsync),
                   ActivitySettings.CommonActivityRetryOptions,
                   exportDataEntity);
        }

        /// <summary>
        /// update the export data.
        /// </summary>
        /// <param name="exportDataEntity">export data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(UpdateExportDataActivityAsync))]
        public async Task UpdateExportDataActivityAsync(
        [ActivityTrigger] ExportDataEntity exportDataEntity)
        {
            await this.exportDataRepository.CreateOrUpdateAsync(exportDataEntity);
        }
    }
}
