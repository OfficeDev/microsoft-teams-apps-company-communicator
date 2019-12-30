// <copyright file="GetRecipientDataListForAllUsersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get recipient data list for all users" durable activity.
    /// </summary>
    public class GetRecipientDataListForAllUsersActivity
    {
        private readonly UserDataRepository userDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForAllUsersActivity"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        public GetRecipientDataListForAllUsersActivity(
            UserDataRepository userDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.userDataRepository = userDataRepository;
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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetAllUsersRecipientDataListAsync))]
        public async Task GetAllUsersRecipientDataListAsync(
            [ActivityTrigger] string notificationDataEntityId)
        {
            var allUsersRecipientDataList = await this.userDataRepository.GetAllAsync();

            await this.sentNotificationDataRepository
                .InitializeSentNotificationDataForRecipientBatchAsync(notificationDataEntityId, allUsersRecipientDataList);
        }
    }
}