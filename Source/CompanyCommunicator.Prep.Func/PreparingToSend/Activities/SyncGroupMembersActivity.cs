// <copyright file="SyncGroupMembersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// Syncs group members to Sent notification table.
    /// </summary>
    public class SyncGroupMembersActivity
    {
        private readonly IGroupMembersService groupMembersService;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IUserDataRepository userDataRepository;
        private readonly IUserTypeService userTypeService;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncGroupMembersActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="notificationDataRepository">Notifications data repository.</param>
        /// <param name="groupMembersService">Group members service.</param>
        /// <param name="userDataRepository">User Data repository.</param>
        /// <param name="userTypeService">User Type service.</param>
        /// <param name="localizer">Localization service.</param>
        public SyncGroupMembersActivity(
            ISentNotificationDataRepository sentNotificationDataRepository,
            INotificationDataRepository notificationDataRepository,
            IGroupMembersService groupMembersService,
            IUserDataRepository userDataRepository,
            IUserTypeService userTypeService,
            IStringLocalizer<Strings> localizer)
        {
            this.groupMembersService = groupMembersService ?? throw new ArgumentNullException(nameof(groupMembersService));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.userTypeService = userTypeService ?? throw new ArgumentNullException(nameof(userTypeService));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Syncs group members to Sent notification table.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>It returns the group transitive members first page and next page url.</returns>
        [FunctionName(FunctionNames.SyncGroupMembersActivity)]
        public async Task<RecipientsInfo> RunAsync(
        [ActivityTrigger](string notificationId, string groupId, int activityIndex) input, ILogger log)
        {
            _ = input.notificationId ?? throw new ArgumentNullException(nameof(input.notificationId));
            _ = input.groupId ?? throw new ArgumentNullException(nameof(input.groupId));
            _ = log ?? throw new ArgumentNullException(nameof(log));

            var notificationId = input.notificationId;
            var groupId = input.groupId;
            var activityIndex = input.activityIndex;

            try
            {
                // Get all members.
                var users = await this.groupMembersService.GetGroupMembersAsync(groupId);

                // Convert to Recipients
                var recipients = await this.GetRecipientsAsync(notificationId, users);

                // Store.
                await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);

                // Store in batches and return batch info.
                return await this.StoreInBatchesAsync(recipients, notificationId, activityIndex);
            }
            catch (Exception ex)
            {
                var errorMessage = this.localizer.GetString("FailedToGetMembersForGroupFormat", groupId, ex.Message);
                log.LogError(ex, errorMessage);
                await this.notificationDataRepository.SaveWarningInNotificationDataEntityAsync(notificationId, errorMessage);
                return default;
            }
        }

        private async Task<RecipientsInfo> StoreInBatchesAsync(IEnumerable<SentNotificationDataEntity> recipients, string notificationId, int activityIndex)
        {
            var recipientBatches = recipients.ToList().SeparateIntoBatches(1000);

            int batchIndex = 1;
            var recipientInfo = new RecipientsInfo
            {
                BatchName = new List<string>(),

                // Update if there is any recipient which has no conversation id.
                IsPendingRecipient = recipients.Any(x => string.IsNullOrEmpty(x.ConversationId)),
            };

            foreach (var recipientBatch in recipientBatches)
            {
                // Update PartitionKey to Batch Key
                recipientBatch.ForEach(s => s.PartitionKey = $"{s.PartitionKey}:{activityIndex}:{batchIndex}");
                recipientInfo.TotalRecipientCount += recipientBatch.Count();

                // Store.
                await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipientBatch);
                recipientInfo.BatchName.Add(recipientBatch.FirstOrDefault().PartitionKey);
                batchIndex++;
            }

            return recipientInfo;
        }

        /// <summary>
        /// Reads corresponding user entity from User table and creates a recipient for every user.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="users">Users.</param>
        /// <returns>List of recipients.</returns>
        private async Task<IEnumerable<SentNotificationDataEntity>> GetRecipientsAsync(string notificationId, IEnumerable<User> users)
        {
            var recipients = new ConcurrentBag<SentNotificationDataEntity>();

            // Get User Entities.
            var maxParallelism = Math.Min(100, users.Count());
            await Task.WhenAll(users.ForEachAsync(maxParallelism, async user =>
            {
                var userEntity = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, user.Id);

                // This is to set the type of user(existing only, new ones will be skipped) to identify later if it is member or guest.
                var userType = user.UserPrincipalName.GetUserType();
                if (userEntity == null && userType.Equals(UserType.Guest, StringComparison.OrdinalIgnoreCase))
                {
                    // Skip processing new Guest users.
                    return;
                }

                await this.userTypeService.UpdateUserTypeForExistingUserAsync(userEntity, userType);
                if (userEntity == null)
                {
                    userEntity = new UserDataEntity()
                    {
                        AadId = user.Id,
                        UserType = userType,
                    };
                }

                recipients.Add(userEntity.CreateInitialSentNotificationDataEntity(partitionKey: notificationId));
            }));

            return recipients;
        }
    }
}
