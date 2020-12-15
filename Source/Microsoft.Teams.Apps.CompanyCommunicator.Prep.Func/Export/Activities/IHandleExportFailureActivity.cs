// <copyright file="IHandleExportFailureActivity.cs" company="Microsoft">
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
    /// Interface for HandleExportFailureActivity.
    /// </summary>
    public interface IHandleExportFailureActivity
    {
        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="exportDataEntity">export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>instance of metadata.</returns>
        public Task RunAsync(
            IDurableOrchestrationContext context,
            ExportDataEntity exportDataEntity,
            ILogger log);

        /// <summary>
        /// This method represents the "clean up" durable activity.
        /// If exceptions happen in the "export" operation,
        /// this method is called to do the clean up work, e.g. delete the files,records and etc.
        /// </summary>
        /// <param name="exportDataEntity">export data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(HandleFailureActivityAsync))]
        public Task HandleFailureActivityAsync([ActivityTrigger] ExportDataEntity exportDataEntity);
    }
}
