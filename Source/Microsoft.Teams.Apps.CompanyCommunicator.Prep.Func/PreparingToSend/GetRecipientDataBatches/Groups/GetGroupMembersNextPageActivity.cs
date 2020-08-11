// <copyright file="GetGroupMembersNextPageActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches.Groups
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This class contains the "get group member next page" durable activity.
    /// This activity prepares the SentNotification data table by filling it with an initialized row
    /// and failed row.
    /// For each recipient - every member of the given group is a recipient.
    /// 1). It gets the recipient data list for a group.
    /// 2). It initializes or fails the sent notification data table with a row for each member in that group.
    /// </summary>
    public class GetGroupMembersNextPageActivity
    {
        private readonly IGroupMembersService groupMembersService;
        private readonly InitializeorFailGroupMembersActivity initializeorFailGroupMembersActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetGroupMembersNextPageActivity"/> class.
        /// </summary>
        /// <param name="groupMembersService">Group members service.</param>
        /// <param name="initializeorFailGroupMembersActivity">Initialize or Fail Group members service.</param>
        public GetGroupMembersNextPageActivity(
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
        /// <param name="groupMembersPage">group members reference page.</param>
        /// <param name="pageUrl">grouup members page url.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<(IGroupTransitiveMembersCollectionWithReferencesPage, string)> RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId,
            IGroupTransitiveMembersCollectionWithReferencesPage groupMembersPage,
            string pageUrl,
            ILogger log)
        {
            var (groupMembersNextPage, nextPageUrl) = await context.
                CallActivityWithRetryAsync
                <(IGroupTransitiveMembersCollectionWithReferencesPage, string)>(
                 nameof(GetGroupMembersNextPageActivity.GetGroupMembersNextPageAsync),
                 ActivitySettings.CommonActivityRetryOptions,
                 (groupMembersPage, pageUrl));

            // intialize the groupMembers if app installed.
            // fail the groupMembers if app is not installed.
            await this.initializeorFailGroupMembersActivity.
                RunAsync(
                context,
                notificationDataEntityId,
                groupMembersNextPage.OfType<User>(),
                log);

            return (groupMembersNextPage, nextPageUrl);
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
            var nextPageUrl = groupMembersPage.AdditionalData.NextPageUrl();
            return (groupMembersPage, nextPageUrl);
        }
    }
}
