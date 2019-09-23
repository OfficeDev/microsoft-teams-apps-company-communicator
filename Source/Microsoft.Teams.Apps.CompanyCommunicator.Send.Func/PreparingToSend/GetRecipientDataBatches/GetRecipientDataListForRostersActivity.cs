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
        /// <param name="metadataProvider">Meta-data Provider instance.</param>
        public GetRecipientDataListForRostersActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Run the activity.
        /// It uses Fan-out / Fan-in pattern to get recipient data list (team rosters) in parallel.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Rosters == null || notificationDataEntity.Rosters.Count() == 0)
            {
                throw new InvalidOperationException("NotificationDataEntity's Rosters property value is null or empty!");
            }

            var teamDataEntityList = await context.CallActivityWithRetryAsync<IEnumerable<TeamDataEntity>>(
                nameof(GetRecipientDataListForRostersActivity.GetTeamDataEntitiesByIdsAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
                notificationDataEntity);

            var tasks = new List<Task>();
            foreach (var teamDataEntity in teamDataEntityList)
            {
                var task = context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                    nameof(GetRecipientDataListForRostersActivity.GetTeamRosterDataAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
                    new GetRecipientDataListForRostersActivityDTO
                    {
                        NotificationDataEntityId = notificationDataEntity.Id,
                        TeamDataEntity = teamDataEntity,
                    });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
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
        /// 1). It gets recipient data list for a team's roster.
        /// 2). Initialize sent notification data in the table storage.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetTeamRosterDataAsync))]
        public async Task GetTeamRosterDataAsync(
            [ActivityTrigger] GetRecipientDataListForRostersActivityDTO input,
            ILogger log)
        {
            try
            {
                var roster = await this.metadataProvider.GetTeamRosterRecipientDataEntityListAsync(
                    input.TeamDataEntity.ServiceUrl,
                    input.TeamDataEntity.TeamId);

                await this.metadataProvider.InitializeStatusInSentNotificationDataAsync(
                    input.NotificationDataEntityId,
                    roster);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.metadataProvider.SaveWarningInNotificationDataEntityAsync(
                    input.NotificationDataEntityId,
                    ex.Message);
            }
        }
    }
}