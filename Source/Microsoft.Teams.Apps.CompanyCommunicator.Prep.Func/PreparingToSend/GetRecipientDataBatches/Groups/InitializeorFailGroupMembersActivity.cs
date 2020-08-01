// <copyright file="InitializeorFailGroupMembersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches.Groups
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;
    using Polly;

    /// <summary>
    /// This class contains the "initialize or fail group member" durable activity.
    /// This activity prepares the SentNotification data table by filling it with an initialized row
    /// and failed row.
    /// It initializes or fails the sent notification data table with a row for each member in that group.
    /// </summary>
    public class InitializeorFailGroupMembersActivity
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly GetUserDataEntitiesByIdsActivity getUserDataEntitiesByIdActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeorFailGroupMembersActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="getUserDataEntitiesByIdActivity">get user data entities by id activity.</param>
        public InitializeorFailGroupMembersActivity(
            SentNotificationDataRepository sentNotificationDataRepository,
            GetUserDataEntitiesByIdsActivity getUserDataEntitiesByIdActivity)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.getUserDataEntitiesByIdActivity = getUserDataEntitiesByIdActivity;
        }

        /// <summary>
        /// Run the activity.
        /// Get recipient data list (group members) in parallel using fan in/fan out pattern.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="groupMembers">list of group members.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<User> groupMembers,
            ILogger log)
        {
            var groupMemberAadIds = groupMembers.Select(user => user.Id);

            // get all installed users.
            var installedUsers = await this.getUserDataEntitiesByIdActivity.
                RunAsync(context, groupMemberAadIds, log);

            // intialize the groupMembers if app installed.
            // fail the groupMembers if app is not installed.
            await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                 nameof(InitializeorFailGroupMembersActivity.InitializeOrFailGroupMembersAsync),
                 ActivitySettings.CommonActivityRetryOptions,
                 (notificationDataEntityId, groupMembers, installedUsers));
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
            int maxNumberOfAttempts = 10;

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

            // Retry it in addition to the original call.
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(maxNumberOfAttempts, p => TimeSpan.FromSeconds(p));
            await retryPolicy.ExecuteAsync(async () =>
            {
                await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(sentNotificationDataEntities);
            });

            // Fail the user in SentNotificationsData Table.
            var failedUsers = groupMembersAndUsersDto.groupMembers.FilterInstalledUsers(groupMembersAndUsersDto.appUsers).Convert();
            var failedsentNotificationDataEntities = failedUsers
              .Select(userDataEntity =>
              {
                  return userDataEntity.CreateFailedSentNotificationDataEntity(
                      partitionKey: groupMembersAndUsersDto.notificationDataEntity);
              });

            // Retry it in addition to the original call.
            var retryFailedUserPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(maxNumberOfAttempts, p => TimeSpan.FromSeconds(p));
            await retryPolicy.ExecuteAsync(async () =>
            {
                await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(failedsentNotificationDataEntities);
            });
        }
    }
}
