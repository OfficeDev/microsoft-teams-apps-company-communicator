// <copyright file="GetRecipientDataListForGroupActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This class contains the "get recipient data list for group" durable activity.
    /// This activity prepares the SentNotification data table by filling it with an initialized row
    /// and failed row.
    /// For each recipient - every member of the given group is a recipient.
    /// 1). It gets the recipient data list for a group.
    /// 2). It initializes or fails the sent notification data table with a row for each member in that group.
    /// </summary>
    public class GetRecipientDataListForGroupActivity
    {
        private readonly string microsoftAppId;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly IGroupMembersService groupMembersService;
        private readonly UserDataRepository userDataRepository;
        private readonly GetUserDataEntitiesByIdsActivity getUserDataEntitiesByIdActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForGroupActivity"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="groupMembersService">Group members service.</param>
        /// <param name="userDataRepository">User data repository.</param>
        /// <param name="getUserDataEntitiesByIdActivity">Get user data by Ids activity.</param>
        public GetRecipientDataListForGroupActivity(
            IOptions<BotOptions> botOptions,
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository,
            IGroupMembersService groupMembersService,
            UserDataRepository userDataRepository,
            GetUserDataEntitiesByIdsActivity getUserDataEntitiesByIdActivity)
        {
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.notificationDataRepository = notificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.groupMembersService = groupMembersService;
            this.userDataRepository = userDataRepository;
            this.getUserDataEntitiesByIdActivity = getUserDataEntitiesByIdActivity;
        }

        /// <summary>
        /// Run the activity.
        /// Get recipient data list (group members) in parallel using fan in/fan out pattern.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="groupId">Group id.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId,
            string groupId,
            ILogger log)
        {
            try
            {
                var (groupMembersPage, nextPageUrl) = await context.
                    CallActivityWithRetryAsync
                    <(IGroupTransitiveMembersCollectionWithReferencesPage, string)>(
                     nameof(GetRecipientDataListForGroupActivity.GetGroupMembersAsync),
                     ActivitySettings.CommonActivityRetryOptions,
                     groupId);

                var groupMembers = groupMembersPage.OfType<User>();
                var groupMemberAadIds = groupMembers.Select(user => user.Id);

                // get all installed users.
                var installedUsers = await this.getUserDataEntitiesByIdActivity.
                    RunAsync(context, groupMemberAadIds, log);

                // intialize the groupMembers if app installed.
                // fail the groupMembers if app is not installed.
                await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                 nameof(GetRecipientDataListForGroupActivity.InitializeOrFailGroupMembersAsync),
                 ActivitySettings.CommonActivityRetryOptions,
                 (notificationDataEntityId, groupMembers, installedUsers));

                while (!string.IsNullOrEmpty(nextPageUrl))
                {
                    (groupMembersPage, nextPageUrl) = await context.
                        CallActivityWithRetryAsync
                        <(IGroupTransitiveMembersCollectionWithReferencesPage, string)>(
                         nameof(GetRecipientDataListForGroupActivity.GetGroupMembersNextPageAsync),
                         ActivitySettings.CommonActivityRetryOptions,
                         (groupMembersPage, nextPageUrl));

                    groupMembers = groupMembersPage.OfType<User>();
                    groupMemberAadIds = groupMembers.Select(user => user.Id);

                    installedUsers = await this.getUserDataEntitiesByIdActivity.
                        RunAsync(context, groupMemberAadIds, log);

                    // intialize the groupMembers if app installed.
                    // fail the groupMembers if app is not installed.
                    await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                     nameof(GetRecipientDataListForGroupActivity.InitializeOrFailGroupMembersAsync),
                     ActivitySettings.CommonActivityRetryOptions,
                     (notificationDataEntityId, groupMembers, installedUsers));
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load members of the group {groupId}: {ex.Message}";

                log.LogError(ex, errorMessage);

                await this.notificationDataRepository
                    .SaveWarningInNotificationDataEntityAsync(notificationDataEntityId, errorMessage);
            }
        }

        /// <summary>
        /// This method represents the "get group members" durable activity.
        /// It gets the group members.
        /// </summary>
        /// <param name="groupId">Group Id.</param>
        /// <returns>It returns the group transitive members first page and next page url.</returns>
        [FunctionName(nameof(GetGroupMembersAsync))]
        public async Task<(IGroupTransitiveMembersCollectionWithReferencesPage, string)> GetGroupMembersAsync(
        [ActivityTrigger] string groupId)
        {
            var groupMembersPage = await this.groupMembersService.
                                            GetGroupMembersPageByIdAsync(groupId);
            var nextPageUrl = this.GetNextPageUrl(groupMembersPage.AdditionalData);
            return (groupMembersPage, nextPageUrl);
        }

        /// <summary>
        /// This method represents the "get group members" durable activity.
        /// It gets the group members.
        /// </summary>
        /// <param name="groupMembersReference">Tuple of groupMembers and next members data url.</param>
        /// <returns>It returns the group transitive members first page and next page url.</returns>
        [FunctionName(nameof(GetGroupMembersNextPageAsync))]
        public async Task<(IGroupTransitiveMembersCollectionWithReferencesPage, string)> GetGroupMembersNextPageAsync(
         [ActivityTrigger](IGroupTransitiveMembersCollectionWithReferencesPage groupMembersPage, string nextPageUrl) groupMembersReference)
        {
            var groupMembersPage = await this.groupMembersService.
                GetGroupMembersNextPageAsnyc(
                groupMembersReference.groupMembersPage, groupMembersReference.nextPageUrl);
            var nextPageUrl = this.GetNextPageUrl(groupMembersPage.AdditionalData);
            return (groupMembersPage, nextPageUrl);
        }

        /// <summary>
        /// This method represents the "initialize or fail user data" durable activity.
        /// </summary>
        /// <param name="groupMembersAndUsersDto">Tuple containing notification data entity, group Members and installed users.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(InitializeOrFailGroupMembersAsync))]
        public async Task InitializeOrFailGroupMembersAsync(
            [ActivityTrigger](
            string notificationDataEntity,
            IEnumerable<User> groupMembers,
            IEnumerable<UserDataEntity> appUsers) groupMembersAndUsersDto)
        {
            // Filter the user not found.
            groupMembersAndUsersDto.appUsers = groupMembersAndUsersDto.appUsers.
                                             Where(user => user != null);

            // Initialize the user SentNotificationsData Table, to be picekd for sending.
            var sentNotificationDataEntities = groupMembersAndUsersDto.appUsers
                .Select(userDataEntity =>
                {
                    return userDataEntity.CreateInitialSentNotificationDataEntity(
                        partitionKey: groupMembersAndUsersDto.notificationDataEntity);
                });
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(sentNotificationDataEntities);

            // Fail the user in SentNotificationsData Table.
            var failedUsers = groupMembersAndUsersDto.groupMembers.Intersect(groupMembersAndUsersDto.appUsers).Convert();
            var failedsentNotificationDataEntities = failedUsers
              .Select(userDataEntity =>
              {
                  return userDataEntity.CreateFailedSentNotificationDataEntity(
                      partitionKey: groupMembersAndUsersDto.notificationDataEntity);
              });
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(failedsentNotificationDataEntities);
        }

        private string GetNextPageUrl(IDictionary<string, object> additionalData)
        {
            additionalData.TryGetValue(Common.Constants.ODataNextPageLink, out object nextLink);
            var nextPageUrl = (nextLink == null) ? string.Empty : nextLink.ToString();
            return nextPageUrl;
        }
    }
}
