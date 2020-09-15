// <copyright file="SyncGroupMembersActivity.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;
    using Polly;

    /// <summary>
    /// Syncs group members to Sent notification table.
    /// </summary>
    public class SyncGroupMembersActivity
    {
        private readonly IGroupMembersService groupMembersService;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncGroupMembersActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="groupMembersService">Group members service.</param>
        /// <param name="userDataRepository">User Data repository.</param>
        public SyncGroupMembersActivity(
            SentNotificationDataRepository sentNotificationDataRepository,
            IGroupMembersService groupMembersService,
            UserDataRepository userDataRepository)
        {
            this.groupMembersService = groupMembersService ?? throw new ArgumentNullException(nameof(groupMembersService));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
        }

        /// <summary>
        /// Syncs group members to Sent notification table.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <returns>It returns the group transitive members first page and next page url.</returns>
        [FunctionName(FunctionNames.SyncGroupMembersActivity)]
        public async Task RunAsync(
        [ActivityTrigger](string notificationId, string groupId) input)
        {
            var notificationId = input.notificationId;
            var groupId = input.groupId;

            // Get all members.
            var users = await this.GetGroupMembersAsync(groupId);

            // Convert to Recipients
            var recipients = await this.GetRecipientsAsync(notificationId, users);

            // Store.
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);
        }

        private async Task<IEnumerable<User>> GetGroupMembersAsync(string groupId)
        {
            var users = new List<User>();

            var groupMembersPage = await this.groupMembersService.
                GetGroupMembersPageByIdAsync(groupId);
            var nextPageUrl = this.GetNextPageUrl(groupMembersPage.AdditionalData);

            users.AddRange(groupMembersPage.OfType<User>());

            while (!string.IsNullOrWhiteSpace(nextPageUrl))
            {
                groupMembersPage = await this.groupMembersService
                    .GetGroupMembersNextPageAsnyc(groupMembersPage, nextPageUrl);
                nextPageUrl = this.GetNextPageUrl(groupMembersPage.AdditionalData);

                users.AddRange(groupMembersPage.OfType<User>());
            }

            return users;
        }

        /// <summary>
        /// Reads corresponding user entity from User table and creates a recipient for every user.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="users">Users.</param>
        /// <returns>List of recipients.</returns>
        private async Task<IEnumerable<SentNotificationDataEntity>> GetRecipientsAsync(string notificationId, IEnumerable<User> users)
        {
            var recipients = new List<SentNotificationDataEntity>();

            // Get User Entities.
            foreach (var user in users)
            {
                var userEntity = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, user.Id);
                if (userEntity == null)
                {
                    userEntity = new UserDataEntity()
                    {
                        AadId = user.Id,
                        Name = user.DisplayName,
                        Email = user.Mail,
                        Upn = user.UserPrincipalName,
                    };
                }

                recipients.Add(userEntity.CreateInitialSentNotificationDataEntity(partitionKey: notificationId));
            }

            return recipients;
        }

        /// <summary>
        /// Extracts the next page url.
        /// </summary>
        /// <param name="additionalData">dictionary contaning odata next page link.</param>
        /// <returns>next page url.</returns>
        private string GetNextPageUrl(IDictionary<string, object> additionalData)
        {
            additionalData.TryGetValue(Common.Constants.ODataNextPageLink, out object nextLink);
            var nextPageUrl = (nextLink == null) ? string.Empty : nextLink.ToString();
            return nextPageUrl;
        }
    }
}
