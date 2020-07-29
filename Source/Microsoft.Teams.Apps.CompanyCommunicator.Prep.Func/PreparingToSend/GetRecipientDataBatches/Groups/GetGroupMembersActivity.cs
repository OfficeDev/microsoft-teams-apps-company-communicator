// <copyright file="GetGroupMembersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches.Groups
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This class contains the "get group member" durable activity.
    /// This activity prepares the SentNotification data table by filling it with an initialized row
    /// and failed row.
    /// For each recipient - every member of the given group is a recipient.
    /// 1). It gets the recipient data list for a group.
    /// 2). It initializes or fails the sent notification data table with a row for each member in that group.
    /// </summary>
    public class GetGroupMembersActivity
    {
        private readonly IGroupMembersService groupMembersService;
        private readonly InitializeorFailGroupMembersActivity initializeorFailGroupMembersActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetGroupMembersActivity"/> class.
        /// </summary>
        /// <param name="groupMembersService">Group members service.</param>
        /// <param name="initializeorFailGroupMembersActivity">Initialize or Fail Group members service.</param>
        public GetGroupMembersActivity(
            IGroupMembersService groupMembersService,
            InitializeorFailGroupMembersActivity initializeorFailGroupMembersActivity)
        {
            this.groupMembersService = groupMembersService;
            this.initializeorFailGroupMembersActivity = initializeorFailGroupMembersActivity;
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
        public async Task<(IGroupTransitiveMembersCollectionWithReferencesPage, string)> RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId,
            string groupId,
            ILogger log)
        {
            var (groupMembersPage, nextPageUrl) = await context.
                CallActivityWithRetryAsync
                <(IGroupTransitiveMembersCollectionWithReferencesPage, string)>(
                 nameof(GetGroupMembersActivity.GetGroupMembersAsync),
                 ActivitySettings.CommonActivityRetryOptions,
                 groupId);

            // intialize the groupMembers if app installed.
            // fail the groupMembers if app is not installed.
            await this.initializeorFailGroupMembersActivity.
                RunAsync(
                context,
                notificationDataEntityId,
                groupMembersPage.OfType<User>(),
                log);

            return (groupMembersPage, nextPageUrl);
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
            var nextPageUrl = groupMembersPage.AdditionalData.NextPageUrl();
            return (groupMembersPage, nextPageUrl);
        }
    }
}
