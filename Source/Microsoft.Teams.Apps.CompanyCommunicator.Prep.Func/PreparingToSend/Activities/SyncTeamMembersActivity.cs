// <copyright file="SyncTeamMembersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// Syncs Team members to SentNotification table.
    /// </summary>
    public class SyncTeamMembersActivity
    {
        private readonly ITeamDataRepository teamDataRepository;
        private readonly ITeamMembersService memberService;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IStringLocalizer<Strings> localizer;
        private readonly IUserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncTeamMembersActivity"/> class.
        /// </summary>
        /// <param name="teamDataRepository">Team Data repository.</param>
        /// <param name="memberService">Teams member service.</param>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="localizer">Localization service.</param>
        /// <param name="userDataRepository">User Data repository.</param>
        public SyncTeamMembersActivity(
            ITeamDataRepository teamDataRepository,
            ITeamMembersService memberService,
            INotificationDataRepository notificationDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository,
            IStringLocalizer<Strings> localizer,
            IUserDataRepository userDataRepository)
        {
            this.teamDataRepository = teamDataRepository ?? throw new ArgumentNullException(nameof(teamDataRepository));
            this.memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
        }

        /// <summary>
        /// Syncs Team members to SentNotification table.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SyncTeamMembersActivity)]
        public async Task RunAsync(
            [ActivityTrigger](string notificationId, string teamId) input,
            ILogger log)
        {
            if (input.notificationId == null)
            {
                throw new ArgumentNullException(nameof(input.notificationId));
            }

            if (input.teamId == null)
            {
                throw new ArgumentNullException(nameof(input.teamId));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var notificationId = input.notificationId;
            var teamId = input.teamId;

            // Read team information.
            var teamInfo = await this.teamDataRepository.GetAsync(TeamDataTableNames.TeamDataPartition, teamId);
            if (teamInfo == null)
            {
                var errorMessage = this.localizer.GetString("FailedToFindTeamInDbFormat", teamId);
                log.LogWarning($"Notification {notificationId}: {errorMessage}");
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                return;
            }

            try
            {
                // Sync members.
                var userEntities = await this.memberService.GetUsersAsync(
                    teamId: teamInfo.TeamId,
                    tenantId: teamInfo.TenantId,
                    serviceUrl: teamInfo.ServiceUrl);

                // Convert to Recipients.
                var recipients = await this.GetRecipientsAsync(notificationId, userEntities);

                // Store.
                await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);
            }
            catch (Exception ex)
            {
                var errorMessage = this.localizer.GetString("FailedToGetMembersForTeamFormat", teamId, ex.Message);
                log.LogError(ex, errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
            }
        }

        /// <summary>
        /// Reads corresponding user entity from User table and creates a recipient for every user.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="users">Users.</param>
        /// <returns>List of recipients.</returns>
        private async Task<IEnumerable<SentNotificationDataEntity>> GetRecipientsAsync(string notificationId, IEnumerable<UserDataEntity> users)
        {
            var recipients = new ConcurrentBag<SentNotificationDataEntity>();

            // Update conversation id from table if available.
            var maxParallelism = Math.Min(100, users.Count());
            await Task.WhenAll(users.ForEachAsync(maxParallelism, async user =>
            {
                var userEntity = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, user.AadId);
                user.ConversationId ??= userEntity?.ConversationId;
                recipients.Add(user.CreateInitialSentNotificationDataEntity(partitionKey: notificationId));
            }));

            return recipients;
        }
    }
}