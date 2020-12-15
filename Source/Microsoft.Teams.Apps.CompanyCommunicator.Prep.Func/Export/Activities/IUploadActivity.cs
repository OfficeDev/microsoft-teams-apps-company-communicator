// <copyright file="IUploadActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;

    /// <summary>
    /// interface for uploads the file to the blob storage.
    /// </summary>
    public interface IUploadActivity
    {
        /// <summary>
        /// Run the activity.
        /// Upload the notification data to Azure Blob storage.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="uploadData">Tuple containing notification data entity,metadata and filename.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task RunAsync(
            IDurableOrchestrationContext context,
            (NotificationDataEntity sentNotificationDataEntity, Metadata metadata, string fileName) uploadData,
            ILogger log);

        /// <summary>
        /// Upload the zip file to blob storage.
        /// </summary>
        /// <param name="uploadData">Tuple containing notification data, metadata and filename.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(UploadActivityAsync))]
        public Task UploadActivityAsync(
            [ActivityTrigger](NotificationDataEntity sentNotificationDataEntity, Metadata metadata, string fileName) uploadData);
    }
}