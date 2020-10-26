// <copyright file="SendFileCardActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Polly;

    /// <summary>
    /// Sends the file card.
    /// </summary>
    public class SendFileCardActivity
    {
        private readonly string microsoftAppId;
        private readonly BotFrameworkHttpAdapter botAdapter;
        private readonly UserDataRepository userDataRepository;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendFileCardActivity"/> class.
        /// </summary>
        /// <param name="botOptions">the bot options.</param>
        /// <param name="botAdapter">the users service.</param>
        /// <param name="userDataRepository">the user data repository.</param>
        /// <param name="localizer">Localization service.</param>
        public SendFileCardActivity(
            IOptions<BotOptions> botOptions,
            BotFrameworkHttpAdapter botAdapter,
            UserDataRepository userDataRepository,
            IStringLocalizer<Strings> localizer)
        {
            this.botAdapter = botAdapter;
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.userDataRepository = userDataRepository;
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Run the activity.
        /// It sends the file card.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="sendData">Tuple containing user id, notification data entity and export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>responsse of send file card acitivity.</returns>
        public async Task<string> RunAsync(
            IDurableOrchestrationContext context,
            (string userId, string notificationId, string fileName) sendData,
            ILogger log)
        {
            return await context.CallActivityWithRetryAsync<string>(
              nameof(SendFileCardActivity.SendFileCardActivityAsync),
              FunctionSettings.DefaultRetryOptions,
              sendData);
        }

        /// <summary>
        /// Sends the file card to the user.
        /// </summary>
        /// <param name="sendData">Tuple containing user id, notification id and filename.</param>
        /// <returns>file card response id.</returns>
        [FunctionName(nameof(SendFileCardActivityAsync))]
        public async Task<string> SendFileCardActivityAsync(
            [ActivityTrigger](string userId, string notificationId, string fileName) sendData)
        {
            var user = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, sendData.userId);

            // Set the service URL in the trusted list to ensure the SDK includes the token in the request.
            MicrosoftAppCredentials.TrustServiceUrl(user.ServiceUrl);

            var conversationReference = new ConversationReference
            {
                ServiceUrl = user.ServiceUrl,
                Conversation = new ConversationAccount
                {
                    Id = user.ConversationId,
                },
            };

            int maxNumberOfAttempts = 10;
            string consentId = string.Empty;
            await this.botAdapter.ContinueConversationAsync(
               botAppId: this.microsoftAppId,
               reference: conversationReference,
               callback: async (turnContext, cancellationToken) =>
               {
                   var fileCardAttachment = this.GetFileCardAttachment(sendData.fileName, sendData.notificationId);
                   var message = MessageFactory.Attachment(fileCardAttachment);

                   // Retry it in addition to the original call.
                   var retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(maxNumberOfAttempts, p => TimeSpan.FromSeconds(p));
                   await retryPolicy.ExecuteAsync(async () =>
                   {
                       var response = await turnContext.SendActivityAsync(message, cancellationToken);
                       consentId = (response == null) ? string.Empty : response.Id;
                   });
               },
               cancellationToken: CancellationToken.None);
            return consentId;
        }

        private Attachment GetFileCardAttachment(string fileName, string notificationId)
        {
            var consentContext = new Dictionary<string, string>
                   {
                       { "filename", fileName },
                       { "notificationId", notificationId },
                   };

            var fileCard = new FileConsentCard
            {
                Description = this.localizer.GetString("FileCardDescription"),
                AcceptContext = consentContext,
                DeclineContext = consentContext,
            };

            var asAttachment = new Attachment
            {
                Content = fileCard,
                ContentType = FileConsentCard.ContentType,
                Name = fileName,
            };

            return asAttachment;
        }
    }
}