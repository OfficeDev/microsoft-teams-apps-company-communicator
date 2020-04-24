// <copyright file="GetRecipientDataListForTeamsActivity.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get recipient data list for teams" durable activity.
    /// This activity prepares the SentNotification data table by filling it with an initialized row
    /// for each recipient - for "teams" every team in the list is a recipient.
    /// 1). It gets the recipient data list of teams ("team general channels").
    /// 2). It initializes the sent notification data table with a row for each team.
    /// </summary>
    public class GetRecipientDataListForTeamsActivity
    {
        private readonly TeamDataRepository teamDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForTeamsActivity"/> class.
        /// </summary>
        /// <param name="teamDataRepository">Team Data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        public GetRecipientDataListForTeamsActivity(
            TeamDataRepository teamDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.teamDataRepository = teamDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Teams == null || notificationDataEntity.Teams.Count() == 0)
            {
                throw new ArgumentException("NotificationDataEntity's Teams property value is null or empty!");
            }

            await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                nameof(GetRecipientDataListForTeamsActivity.GetTeamRecipientDataListAsync),
                ActivitySettings.CommonActivityRetryOptions,
                notificationDataEntity);
        }

        /// <summary>
        /// This method represents the "get recipient data list for teams" durable activity.
        /// 1). It gets the recipient data list of teams ("team general channels").
        /// 2). It initializes the sent notification data table with a row for each team.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetTeamRecipientDataListAsync))]
        public async Task GetTeamRecipientDataListAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(notificationDataEntity.Teams);

            await this.sentNotificationDataRepository
                .InitializeSentNotificationDataForTeamRecipientBatchAsync(notificationDataEntity.Id, teamDataEntities);
        }
    }
}
