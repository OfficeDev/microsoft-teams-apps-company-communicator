// <copyright file="TeamsConversationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;

    /// <summary>
    /// Teams conversation activity.
    /// </summary>
    public class TeamsConversationActivity
    {
        private readonly IConversationService conversationService;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly UserDataRepository userDataRepository;
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsConversationActivity"/> class.
        /// </summary>
        /// <param name="conversationService">The create user conversation service.</param>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="notificationDataRepository">Notification data entity repository.</param>
        public TeamsConversationActivity(
            IConversationService conversationService,
            SentNotificationDataRepository sentNotificationDataRepository,
            UserDataRepository userDataRepository,
            NotificationDataRepository notificationDataRepository)
        {
            this.conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
        }

        /// <summary>
        /// Creates conversation with a Teams user recipient.
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

            // Validate
            if (string.IsNullOrEmpty(recipient?.UserId))
            {
                log.LogError("User id is null or empty.");
                return;
            }

            if (string.IsNullOrEmpty(recipient.ServiceUrl) || string.IsNullOrEmpty(recipient.TenantId))
            {
                log.LogError("Service url or tenant id is null or empty");
                return;
            }

            try
            {
                // Create conversation.
                var response = await this.conversationService.CreateConversationAsync(
                    teamsUserId: recipient.UserId,
                    tenantId: recipient.TenantId,
                    serviceUrl: recipient.ServiceUrl,
                    maxAttempts: 1, // TODO(guptaa): Read from config.
                    log: log);

                // Process response.
                await this.ProcessResponseAsync(recipient, response);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Failed to create conversation for teams user: {recipient?.UserId}. Exception: {exception.Message}";
                log.LogError(exception, errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(input.notificationId, errorMessage);
            }
        }

        /// <summary>
        /// Process create conversation response.
        /// If success:
        /// 1. Updates conversation id.
        /// 2. Updates sent notification table with updated recipient object.
        /// 3. Updates user entity table with updated user details.
        /// </summary>
        /// <param name="recipient">recipient.</param>
        /// <param name="response">Create conversation response.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ProcessResponseAsync(SentNotificationDataEntity recipient, CreateConversationResponse response)
        {
            switch (response.Result)
            {
                case Result.Succeeded:
                    recipient.ConversationId = response.ConversationId;
                    await this.sentNotificationDataRepository.InsertOrMergeAsync(recipient);
                    await this.UpdateUserEntityAsync(recipient);
                    break;

                case Result.Throttled:
                    throw new Exception($"Failed to create conversation. Request throttled. Error message: {response.ErrorMessage}");

                case Result.Failed:
                    throw new Exception($"Failed to create conversation. Error message: {response.ErrorMessage}");
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