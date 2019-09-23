// <copyright file="GetRecipientDataListForTeamsActivity.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get recipient data list for teams" durable activity.
    /// </summary>
    public class GetRecipientDataListForTeamsActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForTeamsActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Meta-data Provider instance.</param>
        public GetRecipientDataListForTeamsActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
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
                throw new InvalidOperationException("NotificationDataEntity's Teams property value is null or empty!");
            }

            await context.CallActivityAsync<IEnumerable<UserDataEntity>>(
                nameof(GetRecipientDataListForTeamsActivity.GetTeamRecipientDataListAsync),
                notificationDataEntity);
        }

        /// <summary>
        /// This method represents the "get recipient data list for teams" durable activity.
        /// 1). It gets recipient data list for teams ("team general channels").
        /// 2). Initialize sent notification data in the table storage.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetTeamRecipientDataListAsync))]
        public async Task GetTeamRecipientDataListAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity,
            ILogger log)
        {
            try
            {
                var teamsRecipientDataList =
                    await this.metadataProvider.GetTeamsRecipientDataEntityListAsync(notificationDataEntity.Teams);

                await this.metadataProvider.InitializeStatusInSentNotificationDataAsync(
                    notificationDataEntity.Id,
                    teamsRecipientDataList);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.metadataProvider.SaveWarningInNotificationDataEntityAsync(
                    notificationDataEntity.Id,
                    ex.Message);
            }
        }
    }
}