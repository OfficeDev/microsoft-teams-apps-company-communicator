// <copyright file="HandleExportFailureActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Polly;

    /// <summary>
    /// This class contains the "clean up" durable activity.
    /// If exceptions happen in the "export" operation, this method is called to clean up and send the error message.
    /// </summary>
    public class HandleExportFailureActivity
    {
        private readonly IExportDataRepository exportDataRepository;
        private readonly IStorageClientFactory storageClientFactory;
        private readonly IUserDataRepository userDataRepository;
        private readonly string authorAppId;
        private readonly CCBotAdapterBase botAdapter;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleExportFailureActivity"/> class.
        /// </summary>
        /// <param name="exportDataRepository">the export data respository.</param>
        /// <param name="storageClientFactory">the storage client factory.</param>
        /// <param name="botOptions">the bot options.</param>
        /// <param name="botAdapter">the users service.</param>
        /// <param name="userDataRepository">the user data repository.</param>
        /// <param name="localizer">Localization service.</param>
        public HandleExportFailureActivity(
            IExportDataRepository exportDataRepository,
            IStorageClientFactory storageClientFactory,
            IOptions<BotOptions> botOptions,
            CCBotAdapterBase botAdapter,
            IUserDataRepository userDataRepository,
            IStringLocalizer<Strings> localizer)
        {
            this.exportDataRepository = exportDataRepository ?? throw new ArgumentNullException(nameof(exportDataRepository));
            this.storageClientFactory = storageClientFactory ?? throw new ArgumentNullException(nameof(storageClientFactory));
            this.botAdapter = botAdapter ?? throw new ArgumentNullException(nameof(botAdapter));
            this.authorAppId = botOptions?.Value?.AuthorAppId ?? throw new ArgumentNullException(nameof(botOptions));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// This method represents the "clean up" durable activity.
        /// If exceptions happen in the "export" operation,
        /// this method is called to do the clean up work, e.g. delete the files,records and etc.
        /// </summary>
        /// <param name="exportDataEntity">export data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.HandleExportFailureActivity)]
        public async Task HandleFailureActivityAsync(
            [ActivityTrigger] ExportDataEntity exportDataEntity)
        {
            if (exportDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportDataEntity));
            }

            await this.DeleteFileAsync(exportDataEntity.FileName);
            await this.SendFailureMessageAsync(exportDataEntity.PartitionKey);
            await this.exportDataRepository.DeleteAsync(exportDataEntity);
        }

        private async Task DeleteFileAsync(string fileName)
        {
            if (fileName == null)
            {
                return;
            }

            var blobContainerClient = this.storageClientFactory.CreateBlobContainerClient(Constants.BlobContainerName);

            await blobContainerClient.CreateIfNotExistsAsync();
            await blobContainerClient
                .GetBlobClient(fileName)
                .DeleteIfExistsAsync();
        }

        private async Task SendFailureMessageAsync(string userId)
        {
            var user = await this.userDataRepository.GetAsync(UserDataTableNames.AuthorDataPartition, userId);

            var conversationReference = new ConversationReference
            {
                ServiceUrl = user.ServiceUrl,
                Conversation = new ConversationAccount
                {
                    Id = user.ConversationId,
                },
            };
            string failureText = this.localizer.GetString("ExportFailureText");

            int maxNumberOfAttempts = 10;
            await this.botAdapter.ContinueConversationAsync(
               botAppId: this.authorAppId,
               reference: conversationReference,
               callback: async (turnContext, cancellationToken) =>
               {
                   // Retry it in addition to the original call.
                   var retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(maxNumberOfAttempts, p => TimeSpan.FromSeconds(p));
                   await retryPolicy.ExecuteAsync(async () =>
                   {
                       var failureMessage = MessageFactory.Text(failureText);
                       failureMessage.TextFormat = "xml";
                       await turnContext.SendActivityAsync(failureMessage, cancellationToken);
                   });
               },
               cancellationToken: CancellationToken.None);
        }
    }
}