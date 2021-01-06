// <copyright file="UpdateExportDataActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;

    /// <summary>
    /// Activity to update export data.
    /// </summary>
    public class UpdateExportDataActivity
    {
        private readonly IExportDataRepository exportDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateExportDataActivity"/> class.
        /// </summary>
        /// <param name="exportDataRepository">the export data respository.</param>
        public UpdateExportDataActivity(IExportDataRepository exportDataRepository)
        {
            this.exportDataRepository = exportDataRepository ?? throw new ArgumentNullException(nameof(exportDataRepository));
        }

        /// <summary>
        /// Run the activity.
        /// It updates the export data.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="exportDataEntity">Export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>Instance of metadata.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            ExportDataEntity exportDataEntity,
            ILogger log)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (exportDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportDataEntity));
            }

            await context.CallActivityWithRetryAsync<Task>(
                                  nameof(UpdateExportDataActivity.UpdateExportDataActivityAsync),
                                  FunctionSettings.DefaultRetryOptions,
                                  exportDataEntity);
        }

        /// <summary>
        /// Update the export data.
        /// </summary>
        /// <param name="exportDataEntity">Export data entity.</param>
        /// <returns>A <see cref="Task"/>Representing the asynchronous operation.</returns>
        [FunctionName(nameof(UpdateExportDataActivityAsync))]
        public async Task UpdateExportDataActivityAsync(
            [ActivityTrigger] ExportDataEntity exportDataEntity)
        {
            if (exportDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportDataEntity));
            }

            await this.exportDataRepository.CreateOrUpdateAsync(exportDataEntity);
        }
    }
}