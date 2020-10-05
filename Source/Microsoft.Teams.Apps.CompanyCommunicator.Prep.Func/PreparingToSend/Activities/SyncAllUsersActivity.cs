// <copyright file="SyncAllUsersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// Syncs all users to Sent notification table.
    /// </summary>
    public class SyncAllUsersActivity
    {
        private readonly UserDataRepository userDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly IUsersService usersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncAllUsersActivity"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="usersService">Users service.</param>
        public SyncAllUsersActivity(
            UserDataRepository userDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository,
            IUsersService usersService)
        {
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        }

        /// <summary>
        /// Syncs all users to Sent notification table.
        /// </summary>
        /// <param name="notification">Notification.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SyncAllUsersActivity)]
        public async Task RunAsync([ActivityTrigger] NotificationDataEntity notification)
        {
            // Sync all users.
            await this.SyncAllUsers();

            // Get users.
            var users = await this.userDataRepository.GetAllAsync();

            // Store in sent notification table.
            var recipients = users.Select(
                user => user.CreateInitialSentNotificationDataEntity(partitionKey: notification.Id));
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);
        }

        /// <summary>
        /// Syncs delta changes only.
        /// </summary>
        private async Task SyncAllUsers()
        {
            // Sync users
            var deltaLink = await this.userDataRepository.GetDeltaLinkAsync();
            (IEnumerable<User>, string) tuple = await this.usersService.GetAllUsersAsync(deltaLink);

            // process users.
            var users = tuple.Item1;
            if (!users.IsNullOrEmpty())
            {
                var maxParallelism = Math.Min(users.Count(), 30);
                await users.ForEachAsync(maxParallelism, this.ProcessUserAsync);
            }

            // Store delta link
            if (!string.IsNullOrEmpty(tuple.Item2))
            {
                await this.userDataRepository.SetDeltaLinkAsync(tuple.Item2);
            }
        }

        private async Task ProcessUserAsync(User user)
        {
            // Delete users who were removed.
            if (user.AdditionalData?.ContainsKey("@removed") == true)
            {
                var localUser = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, user.Id);
                if (localUser != null)
                {
                    await this.userDataRepository.DeleteAsync(localUser);
                }

                return;
            }

            // skip Guest users.
            if (string.Equals(user.UserType, "Guest", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // skip users who do not have teams license.
            try
            {
                var hasTeamsLicense = await this.usersService.HasTeamsLicenseAsync(user.Id);
                if (!hasTeamsLicense)
                {
                    return;
                }
            }
            catch (ServiceException)
            {
                // Failed to get user's license details. Will skip the user.
                return;
            }

            // Store user.
            await this.userDataRepository.InsertOrMergeAsync(
                new UserDataEntity()
                {
                    PartitionKey = UserDataTableNames.UserDataPartition,
                    RowKey = user.Id,
                    AadId = user.Id,
                });
        }
    }
}
