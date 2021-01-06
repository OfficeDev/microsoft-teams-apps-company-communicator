// <copyright file="SyncTeamsActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;

    /// <summary>
    /// Sync teams data to Sent notification table.
    /// </summary>
    public class SyncTeamsActivity
    {
        private readonly ITeamDataRepository teamDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IStringLocalizer<Strings> localizer;
        private readonly INotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncTeamsActivity"/> class.
        /// </summary>
        /// <param name="teamDataRepository">Team Data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="localizer">Localization service.</param>
        /// <param name="notificationDataRepository">Notification data entity repository.</param>
        public SyncTeamsActivity(
            ITeamDataRepository teamDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository,
            IStringLocalizer<Strings> localizer,
            INotificationDataRepository notificationDataRepository)
        {
            this.teamDataRepository = teamDataRepository ?? throw new ArgumentNullException(nameof(teamDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
        }

        /// <summary>
        /// Sync teams data to Sent notification table.
        /// </summary>
        /// <param name="notification">Notification.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SyncTeamsActivity)]
        public async Task RunAsync([ActivityTrigger] NotificationDataEntity notification, ILogger log)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            // Get teams data.
            var teamsData = await this.GetTeamDataEntities(notification.Id, notification.Teams, log);

            // Convert to recipients.
            var recipients = teamsData.Select(teamData => this.ConvertToRecipient(notification.Id, teamData));

            // Store.
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);
        }

        /// <summary>
        /// Reads team data entity for every teamId from Storage.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="teamIds">Team Ids.</param>
        /// <param name="log">Logger.</param>
        /// <returns>Team Data Entities.</returns>
        private async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntities(string notificationId, IEnumerable<string> teamIds, ILogger log)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);

            // Analyzes the team id list used by a notification as recipients.
            // Find the teams in the list that do not exist in DB.
            // Log a warning message for each team that is absent in DB.
            foreach (var teamId in teamIds)
            {
                if (!teamDataEntities.Any(p => p.TeamId == teamId))
                {
                    var errorMessage = this.localizer.GetString("FailedToFindTeamInDbFormat", teamId);
                    log.LogWarning($"Notification {notificationId}: {errorMessage}");
                    await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                }
            }

            return teamDataEntities;
        }

        /// <summary>
        /// Creates recipient from TeamDataEntity.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="team">Team entity.</param>
        /// <returns><see cref="SentNotificationDataEntity"/> object.</returns>
        private SentNotificationDataEntity ConvertToRecipient(string notificationId, TeamDataEntity team)
        {
            return new SentNotificationDataEntity
            {
                PartitionKey = notificationId,
                RowKey = team.TeamId,
                RecipientType = SentNotificationDataEntity.TeamRecipientType,
                RecipientId = team.TeamId,
                StatusCode = SentNotificationDataEntity.InitializationStatusCode,
                ConversationId = team.TeamId,
                TenantId = team.TenantId,
                ServiceUrl = team.ServiceUrl,
            };
        }
    }
}
