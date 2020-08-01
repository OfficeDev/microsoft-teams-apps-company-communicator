// <copyright file="GetRecipientDataListForGroupActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches.Groups;

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
        private readonly GetGroupMembersActivity getGroupMembersActivity;
        private readonly GetGroupMembersNextPageActivity getGroupMembersNextPageActivity;
        private readonly HandleWarningActivity handleWarningActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForGroupActivity"/> class.
        /// </summary>
        /// <param name="getGroupMembersActivity">get group member activity.</param>
        /// <param name="getGroupMembersNextPageActivity">get group member next page activity.</param>
        /// <param name="handleWarningActivity">handle warning activity.</param>
        public GetRecipientDataListForGroupActivity(
            GetGroupMembersActivity getGroupMembersActivity,
            GetGroupMembersNextPageActivity getGroupMembersNextPageActivity,
            HandleWarningActivity handleWarningActivity)
        {
            this.getGroupMembersActivity = getGroupMembersActivity;
            this.getGroupMembersNextPageActivity = getGroupMembersNextPageActivity;
            this.handleWarningActivity = handleWarningActivity;
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
                var (groupMembersPage, nextPageUrl) = await this.getGroupMembersActivity.
                    RunAsync(context, notificationDataEntityId, groupId, log);

                while (!string.IsNullOrEmpty(nextPageUrl))
                {
                    (groupMembersPage, nextPageUrl) = await this.getGroupMembersNextPageActivity.
                        RunAsync(context, notificationDataEntityId, groupMembersPage, nextPageUrl, log);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load members of the group {groupId}: {ex.Message}";

                log.LogError(ex, errorMessage);
                await this.handleWarningActivity.RunAsync(context, notificationDataEntityId, errorMessage);
            }
        }
    }
}
