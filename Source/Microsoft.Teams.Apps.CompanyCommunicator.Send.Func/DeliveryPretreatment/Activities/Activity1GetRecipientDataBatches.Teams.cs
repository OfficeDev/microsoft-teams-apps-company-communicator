// <copyright file="Activity1GetRecipientDataBatches.Teams.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get a notification's recipient (team general channel) data list.
    /// It's used by the durable function framework.
    /// </summary>
    public partial class Activity1GetRecipientDataBatches
    {
        /// <summary>
        /// Get recipient (team general channel) batch list.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamRecipientDataBatchesAsync))]
        public async Task<List<List<UserDataEntity>>> GetTeamRecipientDataBatchesAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var teamsRecipientDataList =
                await this.metadataProvider.GetTeamsRecipientDataEntityList(notificationDataEntity.Teams);

            return this.CreateRecipientDataBatches(teamsRecipientDataList);
        }

        private async Task<List<List<UserDataEntity>>> GetTeamRecipientDataBatchesAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            var teamRecipientDataBatches = await context.CallActivityAsync<List<List<UserDataEntity>>>(
                nameof(Activity1GetRecipientDataBatches.GetTeamRecipientDataBatchesAsync),
                notificationDataEntity);

            await context.CallActivityAsync(
                nameof(Activity1GetRecipientDataBatches.InitializeSentNotificationDataAsync),
                new Activity1GetRecipientDataBatchesDTO
                {
                    RecipientDataBatches = teamRecipientDataBatches,
                    NotificationDataEntityId = notificationDataEntity.Id,
                });

            return teamRecipientDataBatches;
        }
    }
}