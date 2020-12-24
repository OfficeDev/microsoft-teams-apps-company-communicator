// <copyright file="CompanyCommunicatorCleanUpFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Azure.Storage.Blobs;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.FileCardServices;

    /// <summary>
    /// Azure Function App triggered as per scheduled.
    /// Used for house keeping activites.
    /// </summary>
    public class CompanyCommunicatorCleanUpFunction
    {
        private readonly int cleanUpFileOlderThanDays;
        private readonly IExportDataRepository exportDataRepository;
        private readonly IFileCardService fileCardService;
        private readonly BlobContainerClient blobContainerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorCleanUpFunction"/> class.
        /// </summary>
        /// <param name="exportDataRepository">The export data repository.</param>
        /// <param name="blobContainerClient">The Azure Blob storage container client.</param>
        /// <param name="fileCardService">The service to manage the file card.</param>
        /// <param name="cleanUpFileOptions">The options to clean up file.</param>
        public CompanyCommunicatorCleanUpFunction(
            IExportDataRepository exportDataRepository,
            BlobContainerClient blobContainerClient,
            IFileCardService fileCardService,
            IOptions<CleanUpFileOptions> cleanUpFileOptions)
        {
            this.exportDataRepository = exportDataRepository;
            this.fileCardService = fileCardService;
            this.blobContainerClient = blobContainerClient;
            this.cleanUpFileOlderThanDays = int.Parse(cleanUpFileOptions.Value.CleanUpFile);
        }

        /// <summary>
        /// Azure Function App triggered as per scheduled.
        /// Used for house keeping activites.
        /// </summary>
        /// <param name="myTimer">The timer schedule.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorCleanUpFunction")]
        public async Task Run([TimerTrigger("%CleanUpScheduleTriggerTime%")] TimerInfo myTimer, ILogger log)
        {
            var cleanUpDateTime = DateTime.UtcNow.AddDays(-this.cleanUpFileOlderThanDays);
            var exportDataEntities = await this.exportDataRepository.GetAllLessThanDateTimeAsync(cleanUpDateTime);
            exportDataEntities = exportDataEntities.Where(exportDataEntity => exportDataEntity.Status.Equals(ExportStatus.Completed.ToString()));
            await this.DeleteFilesAndCards(exportDataEntities);
            await this.exportDataRepository.BatchDeleteAsync(exportDataEntities);

            log.LogInformation($"Company Communicator Clean Up function executed at: {DateTime.Now}");
        }

        /// <summary>
        /// This deletes the files in Azure Blob storage and file cards sent to users.
        /// </summary>
        /// <param name="exportDataEntities">the list of export data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task DeleteFilesAndCards(IEnumerable<ExportDataEntity> exportDataEntities)
        {
            await this.blobContainerClient.CreateIfNotExistsAsync();

            var tasks = new List<Task>();
            foreach (var exportData in exportDataEntities)
            {
                tasks.Add(this.fileCardService.DeleteAsync(exportData.PartitionKey, exportData.FileConsentId));
                tasks.Add(this.blobContainerClient
                    .GetBlobClient(exportData.FileName)
                    .DeleteIfExistsAsync());
            }

            await Task.WhenAll(tasks);
        }
    }
}
