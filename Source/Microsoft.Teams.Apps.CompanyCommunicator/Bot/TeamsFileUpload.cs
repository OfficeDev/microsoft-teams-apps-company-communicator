// <copyright file="TeamsFileUpload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Storage.Blobs;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Service to upload file to user's one drive.
    /// </summary>
    public class TeamsFileUpload
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly IHttpClientFactory clientFactory;
        private readonly IExportDataRepository exportDataRepository;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsFileUpload"/> class.
        /// </summary>
        /// <param name="clientFactory">http client factory.</param>
        /// <param name="exportDataRepository">Export Data Repository.</param>
        /// <param name="blobContainerClient">azure blob container client.</param>
        /// <param name="localizer">Localization service.</param>
        public TeamsFileUpload(
            IHttpClientFactory clientFactory,
            IExportDataRepository exportDataRepository,
            BlobContainerClient blobContainerClient,
            IStringLocalizer<Strings> localizer)
        {
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            this.exportDataRepository = exportDataRepository ?? throw new ArgumentNullException(nameof(exportDataRepository));
            this.blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Sends file upload complete card.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="fileConsentCardResponse">The accepted response object of File Card.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="notificationId">The notfication id.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task reprsenting asynchronous operation.</returns>
        public async Task FileUploadCompletedAsync(
            ITurnContext turnContext,
            FileConsentCardResponse fileConsentCardResponse,
            string fileName,
            string notificationId,
            CancellationToken cancellationToken)
        {
            // Send the file info card to the user.
            var downloadCard = new FileInfoCard
            {
                UniqueId = fileConsentCardResponse.UploadInfo.UniqueId,
                FileType = fileConsentCardResponse.UploadInfo.FileType,
            };

            var asAttachment = new Attachment
            {
                Content = downloadCard,
                ContentType = FileInfoCard.ContentType,
                Name = fileConsentCardResponse.UploadInfo.Name,
                ContentUrl = fileConsentCardResponse.UploadInfo.ContentUrl,
            };

            await this.CleanUp(turnContext, fileName, notificationId, cancellationToken);

            var reply = MessageFactory.Text(this.localizer.GetString("FileReadyText"));
            reply.TextFormat = "xml";
            reply.Attachments = new List<Attachment> { asAttachment };

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        /// <summary>
        /// Send the file upload failed message.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="notificationId">The notification id.</param>
        /// <param name="error">The error message.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task reprsenting asynchronous operation.</returns>
        public async Task FileUploadFailedAsync(
            ITurnContext turnContext,
            string notificationId,
            string error,
            CancellationToken cancellationToken)
        {
            var exportData = await this.exportDataRepository.GetAsync(
                turnContext.Activity.From?.AadObjectId,
                notificationId);

            if (exportData != null)
            {
                var reply = MessageFactory.Text(this.localizer.GetString("FileUploadErrorText"));
                reply.TextFormat = "xml";
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }
        }

        /// <summary>
        /// Upload the file to user's one drive.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="uploadUrl">The One drive upload url.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task reprsenting asynchronous operation.</returns>
        public async Task UploadToOneDrive(
            string fileName,
            string uploadUrl,
            CancellationToken cancellationToken)
        {
            // Download the file from blob storage.
            var (fileContentStream, fileSize) = await this.DownloadFileAsync(fileName);

            // Upload the file to User's One Drive
            var client = this.clientFactory.CreateClient();
            var fileContent = new StreamContent(fileContentStream);
            fileContent.Headers.ContentLength = fileSize;
            fileContent.Headers.ContentRange = new ContentRangeHeaderValue(0, fileSize - 1, fileSize);
            await client.PutAsync(uploadUrl, fileContent, cancellationToken);
        }

        /// <summary>
        /// Clean up the data such as file, consent card, record.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="notificationId">The notfication id.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task reprsenting asynchronous operation.</returns>
        public async Task CleanUp(
            ITurnContext turnContext,
            string fileName,
            string notificationId,
            CancellationToken cancellationToken)
        {
            var exportData = await this.exportDataRepository.GetAsync(
                turnContext.Activity.From?.AadObjectId,
                notificationId);

            // Clean up activity such as deleting consent card, file from blob storage and table record.
            await turnContext.DeleteActivityAsync(exportData.FileConsentId, cancellationToken);
            await this.DeleteFileAsync(fileName);
            await this.exportDataRepository.DeleteAsync(exportData);
        }

        /// <summary>
        /// Extract the file name and notification id.
        /// </summary>
        /// <param name="fileCardContext">The context of file card response.</param>
        /// <returns>file name and notification id.</returns>
        public (string, string) ExtractInformation(object fileCardContext)
        {
            JToken context = JObject.FromObject(fileCardContext);
            var fileName = context["filename"].ToString();
            var notificationId = context["notificationId"].ToString();
            return (fileName, notificationId);
        }

        private async Task<(Stream, long)> DownloadFileAsync(string fileName)
        {
            // Download the file from blob storage.
            await this.blobContainerClient.CreateIfNotExistsAsync();
            var blobClient = this.blobContainerClient.GetBlobClient(fileName);
            var download = await blobClient.DownloadAsync();

            return (download.Value.Content, download.Value.ContentLength);
        }

        private async Task DeleteFileAsync(string fileName)
        {
            await this.blobContainerClient.CreateIfNotExistsAsync();
            var blobClient = this.blobContainerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}