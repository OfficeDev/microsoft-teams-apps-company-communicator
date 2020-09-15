// <copyright file="SyncAllUsersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// Syncs all users to Sent notification table.
    /// </summary>
    public class SyncAllUsersActivity
    {
        private readonly UserDataRepository userDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncAllUsersActivity"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        public SyncAllUsersActivity(
            UserDataRepository userDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
        }

        /// <summary>
        /// Syncs all users to Sent notification table.
        /// </summary>
        /// <param name="notification">Notification.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SyncAllUsersActivity)]
        public async Task RunAsync([ActivityTrigger] NotificationDataEntity notification)
        {
            // Get users.
            var users = await this.userDataRepository.GetAllAsync();

            // Store.
            var recipients = users.Select(
                user => user.CreateInitialSentNotificationDataEntity(partitionKey: notification.Id));
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);
        }
    }
}
