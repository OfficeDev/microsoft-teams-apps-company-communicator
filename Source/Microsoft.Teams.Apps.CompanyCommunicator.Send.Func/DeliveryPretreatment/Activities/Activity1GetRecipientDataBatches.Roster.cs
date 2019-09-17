// <copyright file="Activity1GetRecipientDataBatches.Roster.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get a notification's recipient (team roster) data list.
    /// It's used by the durable function framework.
    /// </summary>
    public partial class Activity1GetRecipientDataBatches
    {
        /// <summary>
        /// Get recipient (team roster) recipient data list.
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
        /// Get recipient (team roster) batch list.
        /// </summary>
        /// <param name="teamDataEntity">Team data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamRosterDataAsync))]
        public async Task<List<UserDataEntity>> GetTeamRosterDataAsync(
            [ActivityTrigger] TeamDataEntity teamDataEntity)
        {
            var roster = await this.metadataProvider.GetTeamRosterAsync(
                teamDataEntity.ServiceUrl,
                teamDataEntity.TeamId);

            return roster.ToList();
        }

        private async Task<List<List<UserDataEntity>>> GetRosterRecipeintDataBatchesAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            var teamDataEntities = await context.CallActivityAsync<IEnumerable<TeamDataEntity>>(
                nameof(Activity1GetRecipientDataBatches.GetTeamDataEntitiesByIdsAsync),
                notificationDataEntity);

            var tasks = new List<Task<IEnumerable<UserDataEntity>>>();
            foreach (var teamDataEntity in teamDataEntities)
            {
                var task = context.CallActivityAsync<IEnumerable<UserDataEntity>>(
                    nameof(Activity1GetRecipientDataBatches.GetTeamRosterDataAsync),
                    teamDataEntity);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            var recipientDataList = tasks.SelectMany(p => p.Result).ToList();
            var recipientDataBatches = this.CreateRecipientDataBatches(recipientDataList);

            await context.CallActivityAsync(
                nameof(Activity1GetRecipientDataBatches.InitializeSentNotificationDataAsync),
                new Activity1GetRecipientDataBatchesDTO
                {
                    RecipientDataBatches = recipientDataBatches,
                    NotificationDataEntityId = notificationDataEntity.Id,
                });

            return recipientDataBatches;
        }
    }
}