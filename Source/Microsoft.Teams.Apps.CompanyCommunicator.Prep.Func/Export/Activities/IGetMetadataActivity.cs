// <copyright file="IGetMetadataActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;

    /// <summary>
    /// Interface for GetMetaDataActivity.
    /// </summary>
    public interface IGetMetadataActivity
    {
        /// <summary>
        /// Run the activity.
        /// It creates and gets the metadata.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="exportRequiredData">Tuple containing notification data entity and export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>instance of metadata.</returns>
        public Task<Metadata> RunAsync(
            IDurableOrchestrationContext context,
            (NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity) exportRequiredData,
            ILogger log);

        /// <summary>
        /// Create and get the metadata.
        /// </summary>
        /// <param name="exportRequiredData">Tuple containing notification data entity and export data entity.</param>
        /// <returns>instance of metadata.</returns>
        [FunctionName(nameof(GetMetadataActivityAsync))]
        public Task<Metadata> GetMetadataActivityAsync(
            [ActivityTrigger](NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity) exportRequiredData);
    }
}
