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
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
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
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IUserDataRepository userDataRepository;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly IAppManagerService appManagerService;
        private readonly IChatsService chatsService;
        private readonly IAppSettingsService appSettingsService;
        private readonly IStringLocalizer<Strings> localizer;

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
        /// <param name="localizer">Localization service.</param>
        public TeamsConversationActivity(
            IConversationService conversationService,
            ISentNotificationDataRepository sentNotificationDataRepository,
            IUserDataRepository userDataRepository,
            INotificationDataRepository notificationDataRepository,
            IAppManagerService appManagerService,
            IChatsService chatsService,
            IAppSettingsService appSettingsService,
            IOptions<TeamsConversationOptions> options,
            IStringLocalizer<Strings> localizer)
        {
            this.conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.appManagerService = appManagerService ?? throw new ArgumentNullException(nameof(appManagerService));
            this.chatsService = chatsService ?? throw new ArgumentNullException(nameof(chatsService));
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
            if (input.notificationId == null)
            {
                throw new ArgumentNullException(nameof(input.notificationId));
            }

            if (input.recipient == null)
            {
                throw new ArgumentNullException(nameof(input.recipient));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

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
                var response = await this.conversationService.CreateUserConversationAsync(
                    teamsUserId: recipient.UserId,
                    tenantId: recipient.TenantId,
                    serviceUrl: recipient.ServiceUrl,
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
                var errorMessage = this.localizer.GetString("FailedToCreateConversationForUserFormat", recipient?.UserId, exception.Message);
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
                // This may happen if the User app is not added to the organization's app catalog.
                var errorMessage = this.localizer.GetString("UserAppNotFound");
                log.LogError(errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                return string.Empty;
            }

            // Install app.
            try
            {
                await this.appManagerService.InstallAppForUserAsync(appId, recipient.RecipientId);
            }
            catch (ServiceException exception)
            {
                switch (exception.StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        // Note: application is already installed, we should fetch conversation id for this user.
                        log.LogWarning("Application is already installed for the user.");
                        break;
                    case HttpStatusCode.NotFound:
                        // Failed to find the User app in App Catalog. This may happen if the User app is deleted from app catalog.
                        var message = this.localizer.GetString("FailedToFindUserAppInAppCatalog", appId);
                        log.LogError(message);
                        await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, message);

                        // Clear cached user app id. The app may fetch an updated app id next time a message is sent.
                        await this.appSettingsService.DeleteUserAppIdAsync();
                        return string.Empty;
                    default:
                        var errorMessage = this.localizer.GetString("FailedToInstallApplicationForUserFormat", recipient?.RecipientId, exception.Message);
                        log.LogError(exception, errorMessage);
                        await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                        return string.Empty;
                }
            }

            // Get conversation id.
            try
            {
                return await this.chatsService.GetChatThreadIdAsync(recipient.RecipientId, appId);
            }
            catch (ServiceException exception)
            {
                var errorMessage = this.localizer.GetString("FailedToGetConversationForUserFormat", recipient?.UserId, exception.StatusCode, exception.Message);
                log.LogError(exception, errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                return string.Empty;
            }
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