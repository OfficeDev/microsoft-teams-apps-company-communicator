// <copyright file="IUpdateExportDataActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;

    /// <summary>
    /// Interface for UpdateExportDataActivity.
    /// </summary>
    public interface IUpdateExportDataActivity
    {
        /// <summary>
        /// Run the activity.
        /// It updates the export data.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="exportDataEntity">Export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>Instance of metadata.</returns>
        public Task RunAsync(
            IDurableOrchestrationContext context,
            ExportDataEntity exportDataEntity,
            ILogger log);

        /// <summary>
        /// Update the export data.
        /// </summary>
        /// <param name="exportDataEntity">Export data entity.</param>
        /// <returns>A <see cref="Task"/>Representing the asynchronous operation.</returns>
        [FunctionName(nameof(UpdateExportDataActivityAsync))]
        public Task UpdateExportDataActivityAsync(
            [ActivityTrigger] ExportDataEntity exportDataEntity);
    }
}
