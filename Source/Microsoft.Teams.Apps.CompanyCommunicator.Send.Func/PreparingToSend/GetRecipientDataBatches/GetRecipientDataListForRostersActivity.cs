// <copyright file="GetRecipientDataListForRostersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get recipient data list for rosters" durable activity.
    /// </summary>
    public class GetRecipientDataListForRostersActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForRostersActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public GetRecipientDataListForRostersActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Run the activity.
        /// It uses Fan-out / Fan-in pattern to get recipient data list (team rosters) parallely.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns recipient data list.</returns>
        public async Task<IEnumerable<UserDataEntity>> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Rosters == null || notificationDataEntity.Rosters.Count() == 0)
            {
                throw new InvalidOperationException("NotificationDataEntity's Rosters property value is null or empty!");
            }

            var teamDataEntityList = await context.CallActivityAsync<IEnumerable<TeamDataEntity>>(
                nameof(GetRecipientDataListForRostersActivity.GetTeamDataEntitiesByIdsAsync),
                notificationDataEntity);

            var tasks = new List<Task<IEnumerable<UserDataEntity>>>();
            foreach (var teamDataEntity in teamDataEntityList)
            {
                var task = context.CallActivityAsync<IEnumerable<UserDataEntity>>(
                    nameof(GetRecipientDataListForRostersActivity.GetTeamRosterDataAsync),
                    new GetRecipientDataListForRostersActivityDTO
                    {
                        NotificationDataEntityId = notificationDataEntity.Id,
                        TeamDataEntity = teamDataEntity,
                    });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            var recipientDataList = tasks.SelectMany(p => p.Result).ToList();

            return recipientDataList;
        }

        /// <summary>
        /// This method represents the "get team data entity list by id" durable activity.
        /// It gets team data list by ids.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamDataEntitiesByIdsAsync))]
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var teamDataEntities =
                await this.metadataProvider.GetTeamDataEntityListByIdsAsync(notificationDataEntity.Rosters);

            return teamDataEntities;
        }

        /// <summary>
        /// This method represents the "get team's roster" durable activity.
        /// It gets recipient data list for a team's roster.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamRosterDataAsync))]
        public async Task<IEnumerable<UserDataEntity>> GetTeamRosterDataAsync(
            [ActivityTrigger] GetRecipientDataListForRostersActivityDTO input,
            ILogger log)
        {
            try
            {
                var roster = await this.metadataProvider.GetTeamRosterRecipientDataEntityListAsync(
                    input.TeamDataEntity.ServiceUrl,
                    input.TeamDataEntity.TeamId);

                return roster.ToList();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.metadataProvider.SaveWarningInNotificationDataEntityAsync(
                    input.NotificationDataEntityId,
                    ex.Message);

                return new List<UserDataEntity>();
            }
        }
    }
}