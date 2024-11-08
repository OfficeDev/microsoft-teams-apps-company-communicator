// <copyright file="SendFileCardActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
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
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Polly;

    /// <summary>
    /// Sends the file card.
    /// </summary>
    public class SendFileCardActivity
    {
        private readonly string authorAppId;
        private readonly CCBotAdapterBase botAdapter;
        private readonly IUserDataRepository userDataRepository;
        private readonly IConversationService conversationService;
        private readonly TeamsConversationOptions options;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendFileCardActivity"/> class.
        /// </summary>
        /// <param name="botOptions">the bot options.</param>
        /// <param name="botAdapter">the users service.</param>
        /// <param name="userDataRepository">the user data repository.</param>
        /// <param name="conversationService">The create author conversation service.</param>
        /// <param name="options">Teams conversation options.</param>
        /// <param name="notificationDataRepository">Notification data entity repository.</param>
        /// <param name="localizer">Localization service.</param>
        public SendFileCardActivity(
            IOptions<BotOptions> botOptions,
            CCBotAdapterBase botAdapter,
            IUserDataRepository userDataRepository,
            IConversationService conversationService,
            IOptions<TeamsConversationOptions> options,
            INotificationDataRepository notificationDataRepository,
            IStringLocalizer<Strings> localizer)
        {
            this.botAdapter = botAdapter ?? throw new ArgumentNullException(nameof(botAdapter));
            this.authorAppId = botOptions?.Value?.AuthorAppId ?? throw new ArgumentNullException(nameof(botOptions));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Sends the file card to the user.
        /// </summary>
        /// <param name="sendData">Tuple containing user id, notification id and filename.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>file card response id.</returns>
        [FunctionName(FunctionNames.SendFileCardActivity)]
        public async Task<string> SendFileCardActivityAsync(
            [ActivityTrigger](string userId, string notificationId, string fileName) sendData,
            ILogger log)
        {
            var user = await this.userDataRepository.GetAsync(UserDataTableNames.AuthorDataPartition, sendData.userId);
            string conversationId = string.Empty;
            if (!string.IsNullOrEmpty(user.UserId))
            {
                // Create conversation using bot adapter for users with teams user id.
                conversationId = await this.CreateConversationWithTeamsAuthor(sendData.notificationId, user, log);
                user.ConversationId = conversationId;
                await this.userDataRepository.CreateOrUpdateAsync(user);
            }

            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ApplicationException("Conversation Id is empty");
            }

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
               botAppId: this.authorAppId,
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

        private async Task<string> CreateConversationWithTeamsAuthor(
            string notificationId,
            UserDataEntity user,
            ILogger log)
        {
            try
            {
                // Create conversation.
                var response = await this.conversationService.CreateAuthorConversationAsync(
                    teamsUserId: user.UserId,
                    tenantId: user.TenantId,
                    serviceUrl: user.ServiceUrl,
                    maxAttempts: this.options.MaxAttemptsToCreateConversation,
                    log: log);

                return response.Result switch
                {
                    Result.Succeeded => response.ConversationId,
                    Result.Throttled => throw new Exception(this.localizer.GetString("FailedToCreateConversationThrottledFormt", response.ErrorMessage)),
                    _ => throw new Exception(this.localizer.GetString("FailedToCreateConversationFormat", response.ErrorMessage)),
                };
            }
            catch (Exception exception)
            {
                var errorMessage = this.localizer.GetString("FailedToCreateConversationForUserFormat", user?.UserId, exception.Message);
                log.LogError(exception, errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                return null;
            }
        }
    }
}