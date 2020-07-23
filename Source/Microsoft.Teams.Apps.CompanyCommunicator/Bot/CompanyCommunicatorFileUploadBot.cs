// <copyright file="CompanyCommunicatorFileUploadBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Storage.Blobs;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Company Commnunictaor File Upload Bot.
    /// </summary>
    public class CompanyCommunicatorFileUploadBot : TeamsActivityHandler
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly string storageConnectionString;
        private readonly ExportDataRepository exportDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorFileUploadBot"/> class.
        /// </summary>
        /// <param name="clientFactory">http client factory.</param>
        /// <param name="repositoryOptions">options.</param>
        /// <param name="exportDataRepository">Export Data Repository</param>
        public CompanyCommunicatorFileUploadBot(
            IHttpClientFactory clientFactory,
            IOptions<RepositoryOptions> repositoryOptions,
            ExportDataRepository exportDataRepository)
        {
            this.clientFactory = clientFactory;
            this.storageConnectionString = repositoryOptions.Value.StorageAccountConnectionString;
            this.exportDataRepository = exportDataRepository;
        }

        /// <summary>
        /// Invoke when a file upload accept consent activitiy is received from the channel.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="fileConsentCardResponse">The accepted response object of File Card.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task reprsenting asynchronous operation.</returns>
        protected override async Task OnTeamsFileConsentAcceptAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            try
            {
                var (fileName, notificationId) = this.ExtractInformation(fileConsentCardResponse.Context);
                var exportData = await this.exportDataRepository.GetAsync(
                    turnContext.Activity.From?.AadObjectId,
                    notificationId);

                // Download the file from blob storage.
                var blobContainerClient = new BlobContainerClient(this.storageConnectionString, Common.Constants.BlobContainerName);
                await blobContainerClient.CreateIfNotExistsAsync();
                var blobClient = blobContainerClient.GetBlobClient(fileName);
                var client = this.clientFactory.CreateClient();
                var download = await blobClient.DownloadAsync();

                // Upload the file to User's One Drive
                long fileSize = download.Value.ContentLength;
                var fileContent = new StreamContent(download.Value.Content);
                fileContent.Headers.ContentLength = download.Value.ContentLength;
                fileContent.Headers.ContentRange = new ContentRangeHeaderValue(0, fileSize - 1, fileSize);
                await client.PutAsync(fileConsentCardResponse.UploadInfo.UploadUrl, fileContent, cancellationToken);
                await this.FileUploadCompletedAsync(turnContext, fileConsentCardResponse, exportData.FileConsentId, cancellationToken);

                // Delete the file and the record.
                await blobClient.DeleteIfExistsAsync();
                await this.exportDataRepository.DeleteAsync(exportData);
            }
            catch (Exception e)
            {
                await this.FileUploadFailedAsync(turnContext, e.ToString(), cancellationToken);
            }
        }

        /// <summary>
        /// Invoke when a file upload decline consent activitiy is received from the channel.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="fileConsentCardResponse">The declined response object of File Card.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task reprsenting asynchronous operation.</returns>
        protected override async Task OnTeamsFileConsentDeclineAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            var (fileName, notificationId) = this.ExtractInformation(fileConsentCardResponse.Context);
            var exportData = await this.exportDataRepository.GetAsync(
                              turnContext.Activity.From?.AadObjectId,
                              notificationId);

            // Delete the file and record.
            await this.exportDataRepository.DeleteAsync(exportData);
            await this.DeleteFileAsync(fileName);

            // Delete the consent card.
            await turnContext.DeleteActivityAsync(exportData.FileConsentId, cancellationToken);

            var reply = MessageFactory.Text($"Declined. We won't upload file <b>{fileName}</b>.");
            reply.TextFormat = "xml";
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task FileUploadCompletedAsync(ITurnContext turnContext, FileConsentCardResponse fileConsentCardResponse, string responseId, CancellationToken cancellationToken)
        {
            // Delete the consent file card.
            await turnContext.DeleteActivityAsync(responseId, cancellationToken);

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

            var reply = MessageFactory.Text($"<b>File uploaded.</b> Your file <b>{fileConsentCardResponse.UploadInfo.Name}</b> is ready to download");
            reply.TextFormat = "xml";
            reply.Attachments = new List<Attachment> { asAttachment };

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task FileUploadFailedAsync(ITurnContext turnContext, string error, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text($"<b>File upload failed.</b> Error: <pre>{error}</pre>");
            reply.TextFormat = "xml";
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private (string, string) ExtractInformation(object fileCardContext)
        {
            JToken context = JObject.FromObject(fileCardContext);
            var fileName = context["filename"].ToString();
            var notificationId = context["notificationId"].ToString();
            return (fileName, notificationId);
        }

        private async Task DeleteFileAsync(string fileName)
        {
            var blobContainerClient = new BlobContainerClient(this.storageConnectionString, Common.Constants.BlobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            var blobClient = blobContainerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
