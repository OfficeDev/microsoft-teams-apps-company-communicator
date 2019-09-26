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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get recipient data list for all users" durable activity.
    /// </summary>
    public class GetRecipientDataListForAllUsersActivity
    {
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;
        private readonly UserDataRepositoryFactory userDataRepositoryFactory;
        private readonly SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForAllUsersActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory.</param>
        /// <param name="userDataRepositoryFactory">User Data repository service.</param>
        /// <param name="sentNotificationDataRepositoryFactory">Sent notification data repository factory.</param>
        public GetRecipientDataListForAllUsersActivity(
            NotificationDataRepositoryFactory notificationDataRepositoryFactory,
            UserDataRepositoryFactory userDataRepositoryFactory,
            SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory)
        {
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
            this.userDataRepositoryFactory = userDataRepositoryFactory;
            this.sentNotificationDataRepositoryFactory = sentNotificationDataRepositoryFactory;
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
            await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                nameof(GetRecipientDataListForAllUsersActivity.GetAllUsersRecipientDataListAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
                notificationDataEntity.Id);
        }

        /// <summary>
        /// This method represents the "get recipient data list for all users" durable activity.
        /// 1). It gets recipient data list for all users.
        /// 2). Initialize sent notification data in the table storage.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetAllUsersRecipientDataListAsync))]
        public async Task GetAllUsersRecipientDataListAsync(
            [ActivityTrigger] string notificationDataEntityId,
            ILogger log)
        {
            try
            {
                var allUersRecipientDataList = await this.userDataRepositoryFactory.CreateRepository(true).GetAllAsync();

                await this.sentNotificationDataRepositoryFactory.CreateRepository(true)
                    .InitializeSentNotificationDataForRecipientBatchAsync(notificationDataEntityId, allUersRecipientDataList);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.notificationDataRepositoryFactory.CreateRepository(true)
                    .SaveWarningInNotificationDataEntityAsync(notificationDataEntityId, ex.Message);
            }
        }
    }
}