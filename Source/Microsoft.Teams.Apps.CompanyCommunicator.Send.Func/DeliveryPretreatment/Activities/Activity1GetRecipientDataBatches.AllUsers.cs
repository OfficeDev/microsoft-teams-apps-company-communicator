// <copyright file="Activity1GetRecipientDataBatches.AllUsers.cs" company="Microsoft">
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
    /// Get a notification's recipient (all users) data list.
    /// It's used by the durable function framework.
    /// </summary>
    public partial class Activity1GetRecipientDataBatches
    {
        /// <summary>
        /// Get recipient (all users) data list.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetAllUsersRecipientDataBatchesAsync))]
        public async Task<List<List<UserDataEntity>>> GetAllUsersRecipientDataBatchesAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var allUersRecipientDataList = await this.metadataProvider.GetUserDataEntityListAsync();

            return this.CreateRecipientDataBatches(allUersRecipientDataList);
        }

        private async Task<List<List<UserDataEntity>>> GetAllUsersRecipientDataBatchesAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            var allUersRecipientDataBatches = await context.CallActivityAsync<List<List<UserDataEntity>>>(
                nameof(Activity1GetRecipientDataBatches.GetAllUsersRecipientDataBatchesAsync),
                notificationDataEntity);

            await context.CallActivityAsync(
                nameof(Activity1GetRecipientDataBatches.InitializeSentNotificationDataAsync),
                new Activity1GetRecipientDataBatchesDTO
                {
                     RecipientDataBatches = allUersRecipientDataBatches,
                     NotificationDataEntityId = notificationDataEntity.Id,
                });

            return allUersRecipientDataBatches;
        }
    }
}