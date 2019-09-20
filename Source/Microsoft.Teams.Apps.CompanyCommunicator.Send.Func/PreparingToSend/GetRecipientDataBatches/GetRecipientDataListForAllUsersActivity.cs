// <copyright file="GetRecipientDataListForAllUsersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get recipient data list for all users" durable activity.
    /// </summary>
    public class GetRecipientDataListForAllUsersActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForAllUsersActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public GetRecipientDataListForAllUsersActivity(MetadataProvider metadataProvider)
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
            var allUersRecipientDataList = await context.CallActivityAsync<IEnumerable<UserDataEntity>>(
                nameof(GetRecipientDataListForAllUsersActivity.GetAllUsersRecipientDataListAsync),
                notificationDataEntity.Id);

            return allUersRecipientDataList;
        }

        /// <summary>
        /// This method represents the "get recipient data list for all users" durable activity.
        /// It gets recipient data list for all users.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetAllUsersRecipientDataListAsync))]
        public async Task<IEnumerable<UserDataEntity>> GetAllUsersRecipientDataListAsync(
            [ActivityTrigger] string notificationDataEntityId,
            ILogger log)
        {
            try
            {
                var allUersRecipientDataList = await this.metadataProvider.GetUserDataEntityListAsync();

                return allUersRecipientDataList;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.metadataProvider.SaveWarningInNotificationDataEntityAsync(
                    notificationDataEntityId,
                    ex.Message);

                return new List<UserDataEntity>();
            }
        }
    }
}