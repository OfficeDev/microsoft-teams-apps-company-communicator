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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// This class contains the "get team data entities by ids" durable activity.
    /// It retrieves team data entities by ids contained in NotificationDataEntity.Rosters property.
    /// </summary>
    public class GetTeamDataEntitiesByIdsActivity
    {
        private readonly TeamDataRepositoryFactory teamDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTeamDataEntitiesByIdsActivity"/> class.
        /// </summary>
        /// <param name="teamDataRepositoryFactory">Team Data repository service.</param>
        public GetTeamDataEntitiesByIdsActivity(
            TeamDataRepositoryFactory teamDataRepositoryFactory)
        {
            this.teamDataRepositoryFactory = teamDataRepositoryFactory;
        }

        /// <summary>
        /// Run the activity.
        /// It retrieves team data entities by ids contained in NotificationDataEntity.Rosters property.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IEnumerable<TeamDataEntity>> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Rosters == null || notificationDataEntity.Rosters.Count() == 0)
            {
                throw new InvalidOperationException("NotificationDataEntity's Rosters property value is null or empty!");
            }

            var teamDataEntityList = await context.CallActivityWithRetryAsync<IEnumerable<TeamDataEntity>>(
                nameof(GetTeamDataEntitiesByIdsActivity.GetTeamDataEntitiesByIdsAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
                notificationDataEntity);

            return teamDataEntityList;
        }

        /// <summary>
        /// This method represents the "get team data entities by ids" durable activity.
        /// It gets team data list by ids.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamDataEntitiesByIdsAsync))]
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var teamIds = notificationDataEntity.Rosters;

            var teamDataEntities =
                await this.teamDataRepositoryFactory.CreateRepository(true).GetTeamDataEntitiesByIdsAsync(teamIds);

            return teamDataEntities;
        }
    }
}