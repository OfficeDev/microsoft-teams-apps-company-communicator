// <copyright file="GetTeamDataEntitiesByIdsActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;

    /// <summary>
    /// This class contains the "get team data entities by ids" durable activity.
    /// It retrieves team data entities by ids.
    ///
    /// The durable activity intends to fix the following issue:
    /// When the system is creating batches for the batch table, if something fails and that Azure Function retries itself
    /// AND in that amount of time the batches have changed (e.g. a new user is added to the data table), then the batches
    /// will fail to send because they will have more than 100 recipients in them.
    ///
    /// The durable activity gets the teams data entity list stored in the teams data table.
    /// The Durable Function persists the activity's result, which is the teams' data entity list, after the activity being
    /// executed successfully first time (for a specific notification).
    /// When retries happen, the activity will reuse the persisted data instead of retrieving it again from DB.
    ///
    /// We maintain idem-potency between retries by using the activity. So that the issue described above can be solved.
    /// </summary>
    public class GetTeamDataEntitiesByIdsActivity
    {
        private readonly TeamDataRepository teamDataRepository;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTeamDataEntitiesByIdsActivity"/> class.
        /// </summary>
        /// <param name="teamDataRepository">Team Data repository.</param>
        /// <param name="notificationDataRepository">Notification data entity repository.</param>
        /// <param name="localizer">Localization service.</param>
        public GetTeamDataEntitiesByIdsActivity(
            TeamDataRepository teamDataRepository,
            NotificationDataRepository notificationDataRepository,
            IStringLocalizer<Strings> localizer)
        {
            this.teamDataRepository = teamDataRepository;
            this.notificationDataRepository = notificationDataRepository;
            this.localizer = localizer;
        }

        /// <summary>
        /// Run the activity.
        /// It retrieves team data entities by ids, i.e. Notification.Rosters or Notification.Teams.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">A notification data entity's ID.</param>
        /// <param name="teamIds">Team id list.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IEnumerable<TeamDataEntity>> RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<string> teamIds)
        {
            if (teamIds == null || !teamIds.Any())
            {
                throw new ArgumentException("Team id list is null or empty!");
            }

            var dto = new GetTeamDataEntitiesByIdsActivityDTO
            {
                NotificationDataEntityId = notificationDataEntityId,
                TeamIds = teamIds,
            };

            var teamDataEntityList = await context.CallActivityWithRetryAsync<IEnumerable<TeamDataEntity>>(
                nameof(GetTeamDataEntitiesByIdsActivity.GetTeamDataEntitiesByIdsAsync),
                ActivitySettings.CommonActivityRetryOptions,
                dto);

            return teamDataEntityList;
        }

        /// <summary>
        /// This method represents the "get team data entities by ids" durable activity.
        /// It gets team data list by ids.
        /// </summary>
        /// <param name="dto">GetTeamDataEntitiesByIdsActivityDTO instance.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamDataEntitiesByIdsAsync))]
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(
            [ActivityTrigger] GetTeamDataEntitiesByIdsActivityDTO dto,
            ILogger log)
        {
            var teamDataEntities =
                await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(dto.TeamIds);

            // Analyzes the team id list used by a notification as recipients.
            // Find the teams in the list that do not exist in DB.
            // Log a warning message for each team that is absent in DB.
            foreach (var teamId in dto.TeamIds)
            {
                if (!teamDataEntities.Any(p => p.TeamId == teamId))
                {
                    var format = this.localizer.GetString("FailedToFindTeamInDbFormat");
                    var errorMessage = string.Format(format, teamId);
                    log.LogWarning($"Notification {dto.NotificationDataEntityId}: {errorMessage}");
                    await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(
                        dto.NotificationDataEntityId,
                        errorMessage);
                }
            }

            return teamDataEntities;
        }
    }
}