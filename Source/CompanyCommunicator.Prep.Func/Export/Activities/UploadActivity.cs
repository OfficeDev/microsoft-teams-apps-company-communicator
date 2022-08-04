// <copyright file="UploadActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
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
    using global::Azure.Storage.Blobs.Models;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
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
        private readonly IStorageClientFactory storageClientFactory;
        private readonly IDataStreamFacade userDataStream;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadActivity"/> class.
        /// </summary>
        /// <param name="storageClientFactory">the storage client factory.</param>
        /// <param name="userDataStream">the user data stream.</param>
        /// <param name="localizer">Localization service.</param>
        public UploadActivity(
            IStorageClientFactory storageClientFactory,
            IDataStreamFacade userDataStream,
            IStringLocalizer<Strings> localizer)
        {
            this.storageClientFactory = storageClientFactory ?? throw new ArgumentNullException(nameof(storageClientFactory));
            this.userDataStream = userDataStream ?? throw new ArgumentNullException(nameof(userDataStream));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Upload the zip file to blob storage.
        /// </summary>
        /// <param name="uploadData">Tuple containing notification data, metadata and filename.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.UploadActivity)]
        public async Task UploadActivityAsync(
            [ActivityTrigger](NotificationDataEntity sentNotificationDataEntity, Metadata metadata, string fileName) uploadData)
        {
            if (uploadData.sentNotificationDataEntity == null)
            {
                throw new ArgumentNullException(nameof(uploadData.sentNotificationDataEntity));
            }

            if (uploadData.metadata == null)
            {
                throw new ArgumentNullException(nameof(uploadData.metadata));
            }

            if (uploadData.fileName == null)
            {
                throw new ArgumentNullException(nameof(uploadData.fileName));
            }

            var blobContainerClient = this.storageClientFactory.CreateBlobContainerClient(Constants.BlobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            await blobContainerClient.SetAccessPolicyAsync(PublicAccessType.None);
            var blob = blobContainerClient.GetBlobClient(uploadData.fileName);

            using var memorystream = new MemoryStream();
            using (var archive = new ZipArchive(memorystream, ZipArchiveMode.Create, true))
            {
                // metadata CSV creation.
                var metadataFileName = string.Concat(this.localizer.GetString("FileName_Metadata"), ".csv");
                var metadataFile = archive.CreateEntry(metadataFileName, CompressionLevel.Optimal);
                using (var entryStream = metadataFile.Open())
                {
                    using var writer = new StreamWriter(entryStream, System.Text.Encoding.UTF8);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
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
                {
                    using (var writer = new StreamWriter(entryStream, System.Text.Encoding.UTF8))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        if (uploadData.sentNotificationDataEntity.Teams.Any())
                        {
                            var teamDataMap = new TeamDataMap(this.localizer);
                            csv.Configuration.RegisterClassMap(teamDataMap);
                            var teamDataStream = this.userDataStream.GetTeamDataStreamAsync(uploadData.sentNotificationDataEntity.Id, uploadData.sentNotificationDataEntity.Status);
                            await foreach (var data in teamDataStream)
                            {
                                await csv.WriteRecordsAsync(data);
                            }
                        }
                        else
                        {
                            var userDataMap = new UserDataMap(this.localizer);
                            csv.Configuration.RegisterClassMap(userDataMap);
                            var userDataStream = this.userDataStream.GetUserDataStreamAsync(uploadData.sentNotificationDataEntity.Id, uploadData.sentNotificationDataEntity.Status);
                            await foreach (var data in userDataStream)
                            {
                                await csv.WriteRecordsAsync(data);
                            }
                        }
                    }
                }
            }

            memorystream.Position = 0;
            await blob.UploadAsync(memorystream, true);
        }
    }
}