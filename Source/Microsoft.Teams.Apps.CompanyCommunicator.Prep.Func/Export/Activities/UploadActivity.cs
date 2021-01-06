// <copyright file="UploadActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using CsvHelper;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Mappers;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;

    /// <summary>
    /// Uploads the file to the blob storage.
    /// </summary>
    public class UploadActivity
    {
        private readonly string storageConnectionString;
        private readonly IDataStreamFacade userDataStream;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadActivity"/> class.
        /// </summary>
        /// <param name="repositoryOptions">the repository options.</param>
        /// <param name="userDataStream">the user data stream.</param>
        /// <param name="localizer">Localization service.</param>
        public UploadActivity(
            IOptions<RepositoryOptions> repositoryOptions,
            IDataStreamFacade userDataStream,
            IStringLocalizer<Strings> localizer)
        {
            this.storageConnectionString = repositoryOptions.Value.StorageAccountConnectionString;
            this.userDataStream = userDataStream;
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        private TimeSpan BackOffPeriod { get; set; } = TimeSpan.FromSeconds(3);

        private int MaxRetry { get; set; } = 15;

        /// <summary>
        /// Run the activity.
        /// Upload the notification data to Azure Blob storage.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="uploadData">Tuple containing notification data entity,metadata and filename.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            (NotificationDataEntity sentNotificationDataEntity, Metadata metadata, string fileName) uploadData,
            ILogger log)
        {
            await context.CallActivityWithRetryAsync(
              nameof(UploadActivity.UploadActivityAsync),
              FunctionSettings.DefaultRetryOptions,
              uploadData);
        }

        /// <summary>
        /// Upload the zip file to blob storage.
        /// </summary>
        /// <param name="uploadData">Tuple containing notification data, metadata and filename.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(UploadActivityAsync))]
        public async Task UploadActivityAsync(
            [ActivityTrigger] (NotificationDataEntity sentNotificationDataEntity, Metadata metadata, string fileName) uploadData)
        {
            CloudStorageAccount storage = CloudStorageAccount.Parse(this.storageConnectionString);
            CloudBlobClient client = storage.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(Common.Constants.BlobContainerName);
            await container.CreateIfNotExistsAsync();

            // Set the permissions so the blobs are private.
            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Off,
            };
            await container.SetPermissionsAsync(permissions);
            CloudBlockBlob blob = container.GetBlockBlobReference(uploadData.fileName);
            var blobRequestOptions = new BlobRequestOptions()
            {
                RetryPolicy = new ExponentialRetry(this.BackOffPeriod, this.MaxRetry),
                SingleBlobUploadThresholdInBytes = 1024 * 1024 * 4, // 4Mb.
                ParallelOperationThreadCount = 1, // Advised to keep 1 if upload size is less than 256 Mb.
            };

            using var memorystream = await blob.OpenWriteAsync(new AccessCondition(), blobRequestOptions, new OperationContext());
            using var archive = new ZipArchive(memorystream, ZipArchiveMode.Create);

            // metadata CSV creation.
            var metadataFileName = string.Concat(this.localizer.GetString("FileName_Metadata"), ".csv");
            var metadataFile = archive.CreateEntry(metadataFileName, CompressionLevel.Optimal);
            using (var entryStream = metadataFile.Open())
            using (var writer = new StreamWriter(entryStream, System.Text.Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var metadataMap = new MetadataMap(this.localizer);
                csv.Configuration.RegisterClassMap(metadataMap);
                csv.WriteHeader(typeof(Metadata));
                await csv.NextRecordAsync();
                csv.WriteRecord(uploadData.metadata);
            }

            // message delivery csv creation.
            var messageDeliveryFileName = string.Concat(this.localizer.GetString("FileName_Message_Delivery"), ".csv");
            var messageDeliveryFile = archive.CreateEntry(messageDeliveryFileName, CompressionLevel.Optimal);
            using (var entryStream = messageDeliveryFile.Open())
            using (var writer = new StreamWriter(entryStream, System.Text.Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                if (uploadData.sentNotificationDataEntity.Teams.Any())
                {
                    var teamDataMap = new TeamDataMap(this.localizer);
                    csv.Configuration.RegisterClassMap(teamDataMap);
                    var teamDataStream = this.userDataStream.GetTeamDataStreamAsync(uploadData.sentNotificationDataEntity.Id);
                    await foreach (var data in teamDataStream)
                    {
                        await csv.WriteRecordsAsync(data);
                    }
                }
                else
                {
                    var userDataMap = new UserDataMap(this.localizer);
                    csv.Configuration.RegisterClassMap(userDataMap);
                    var userDataStream = this.userDataStream.GetUserDataStreamAsync(uploadData.sentNotificationDataEntity.Id);
                    await foreach (var data in userDataStream)
                    {
                        await csv.WriteRecordsAsync(data);
                    }
                }
            }
        }
    }
}
