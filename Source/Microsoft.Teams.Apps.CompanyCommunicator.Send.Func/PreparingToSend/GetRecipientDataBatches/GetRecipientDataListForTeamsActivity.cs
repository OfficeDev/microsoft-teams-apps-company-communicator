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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get a notification's recipient (team general channel) data list.
    /// It's used by the durable function framework.
    /// </summary>
    public partial class GetRecipientDataListForTeamsActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForTeamsActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public GetRecipientDataListForTeamsActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns recipient data list.</returns>
        public async Task<IEnumerable<UserDataEntity>> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Teams == null || notificationDataEntity.Teams.Count() == 0)
            {
                throw new InvalidOperationException("NotificationDataEntity's Teams property value is null or empty!");
            }

            var teamRecipientDataList = await context.CallActivityAsync<IEnumerable<UserDataEntity>>(
                nameof(GetRecipientDataListForTeamsActivity.GetTeamRecipientDataListAsync),
                notificationDataEntity);

            return teamRecipientDataList;
        }

        /// <summary>
        /// Get recipient (team general channel) batch list.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamRecipientDataListAsync))]
        public async Task<IEnumerable<UserDataEntity>> GetTeamRecipientDataListAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var teamsRecipientDataList =
                await this.metadataProvider.GetTeamsRecipientDataEntityList(notificationDataEntity.Teams);

            return teamsRecipientDataList;
        }
    }
}