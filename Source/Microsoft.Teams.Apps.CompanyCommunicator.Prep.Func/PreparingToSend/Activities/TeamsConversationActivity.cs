// <copyright file="TeamsConversationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;

    /// <summary>
    /// Teams conversation activity.
    /// </summary>
    public class TeamsConversationActivity
    {
        private readonly TeamsConversationOptions options;
        private readonly IConversationService conversationService;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly UserDataRepository userDataRepository;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly IAppManagerService appManagerService;
        private readonly IChatsService chatsService;
        private readonly IAppSettingsService appSettingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsConversationActivity"/> class.
        /// </summary>
        /// <param name="conversationService">The create user conversation service.</param>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="notificationDataRepository">Notification data entity repository.</param>
        /// <param name="appManagerService">App manager service.</param>
        /// <param name="chatsService">Chats service.</param>
        /// <param name="appSettingsService">App Settings service.</param>
        /// <param name="options">Teams conversation options.</param>
        public TeamsConversationActivity(
            IConversationService conversationService,
            SentNotificationDataRepository sentNotificationDataRepository,
            UserDataRepository userDataRepository,
            NotificationDataRepository notificationDataRepository,
            IAppManagerService appManagerService,
            IChatsService chatsService,
            IAppSettingsService appSettingsService,
            IOptions<TeamsConversationOptions> options)
        {
            this.conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.appManagerService = appManagerService ?? throw new ArgumentNullException(nameof(appManagerService));
            this.chatsService = chatsService ?? throw new ArgumentNullException(nameof(chatsService));
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates conversation with a pending recipient.
        ///
        /// For teams users - it creates a conversation using bot adapter.
        /// For other users - it installs User application and gets conversation id.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.TeamsConversationActivity)]
        public async Task CreateConversationAsync(
            [ActivityTrigger](string notificationId, SentNotificationDataEntity recipient) input,
            ILogger log)
        {
            var recipient = input.recipient;

            // No-op for Team recipient.
            if (recipient.RecipientType == SentNotificationDataEntity.TeamRecipientType)
            {
                return;
            }

            // create conversation.
            string conversationId;
            if (!string.IsNullOrEmpty(recipient.UserId))
            {
                // Create conversation using bot adapter for users with teams user id.
                conversationId = await this.CreateConversationWithTeamsUser(input.notificationId, recipient, log);
            }
            else
            {
                // check if proactive app installation is enabled.
                if (!this.options.ProactivelyInstallUserApp)
                {
                    log.LogInformation("Proactive app installation is disabled.");
                    return;
                }

                // For other user, install the User's app and get conversation id.
                conversationId = await this.InstallAppAndGetConversationId(input.notificationId, recipient, log);
            }

            if (string.IsNullOrEmpty(conversationId))
            {
                return;
            }

            // Update conversation Id.
            recipient.ConversationId = conversationId;

            // Update service url from cache.
            if (string.IsNullOrEmpty(recipient.ServiceUrl))
            {
                recipient.ServiceUrl = await this.appSettingsService.GetServiceUrlAsync();
            }

            // Update sent notification and user entity.
            await this.sentNotificationDataRepository.InsertOrMergeAsync(recipient);
            await this.UpdateUserEntityAsync(recipient);
        }

        private async Task<string> CreateConversationWithTeamsUser(
            string notificationId,
            SentNotificationDataEntity recipient,
            ILogger log)
        {
            try
            {
                // Create conversation.
                var response = await this.conversationService.CreateConversationAsync(
                    teamsUserId: recipient.UserId,
                    tenantId: recipient.TenantId,
                    serviceUrl: recipient.ServiceUrl,
                    maxAttempts: this.options.MaxAttemptsToCreateConversation,
                    log: log);

                return response.Result switch
                {
                    Result.Succeeded => response.ConversationId,
                    Result.Throttled => throw new Exception($"Failed to create conversation. Request throttled. Error message: {response.ErrorMessage}"),
                    _ => throw new Exception($"Failed to create conversation. Error message: {response.ErrorMessage}"),
                };
            }
            catch (Exception exception)
            {
                var errorMessage = $"Failed to create conversation with teams user: {recipient?.UserId}. Exception: {exception.Message}";
                log.LogError(exception, errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                return null;
            }
        }

        private async Task<string> InstallAppAndGetConversationId(
            string notificationId,
            SentNotificationDataEntity recipient,
            ILogger log)
        {
            var appId = await this.appSettingsService.GetUserAppIdAsync();
            if (string.IsNullOrEmpty(appId))
            {
                log.LogError("User app id not available.");
                return null;
            }

            try
            {
                await this.appManagerService.InstallAppForUserAsync(appId, recipient.RecipientId);
            }
            catch (ServiceException exception)
            {
                switch (exception.StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        log.LogWarning("Application is already installed for the user.");
                        break;

                    case HttpStatusCode.TooManyRequests:
                        log.LogWarning("Application install request throttled.");
                        throw exception;

                    default:
                        var errorMessage = $"Failed to install application for user: {recipient?.UserId}. Exception: {exception.Message}";
                        log.LogError(exception, errorMessage);
                        await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                        return null;
                }
            }

            var conversationId = await this.chatsService.GetChatThreadIdAsync(recipient.RecipientId, appId);
            return conversationId;
        }

        /// <summary>
        /// Updates user entity table.
        /// </summary>
        /// <param name="recipient">Recipient.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task UpdateUserEntityAsync(SentNotificationDataEntity recipient)
        {
            var user = new UserDataEntity()
            {
                PartitionKey = UserDataTableNames.UserDataPartition,
                RowKey = recipient.RecipientId,
                AadId = recipient.RecipientId,
                UserId = recipient.UserId,
                ConversationId = recipient.ConversationId,
                ServiceUrl = recipient.ServiceUrl,
                TenantId = recipient.TenantId,
            };

            await this.userDataRepository.InsertOrMergeAsync(user);
        }
    }
}