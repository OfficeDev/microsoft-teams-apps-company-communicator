// <copyright file="SyncRecipientsOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Syncs target set of recipients to Sent notification table.
    /// </summary>
    public static class SyncRecipientsOrchestrator
    {
        /// <summary>
        /// Fetch recipients and store them in Azure storage.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SyncRecipientsOrchestrator)]
        public static async Task<RecipientsInfo> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var notification = context.GetInput<NotificationDataEntity>();

            // Update notification status.
            await context.CallActivityWithRetryAsync(
                FunctionNames.UpdateNotificationStatusActivity,
                FunctionSettings.DefaultRetryOptions,
                (notification.Id, NotificationStatus.SyncingRecipients));

            // All users.
            if (notification.AllUsers)
            {
                return await context.CallActivityWithRetryAsync<RecipientsInfo>(
                    FunctionNames.SyncAllUsersActivity,
                    FunctionSettings.DefaultRetryOptions,
                    notification);
            }

            // Members of specific teams.
            if (notification.Rosters.Any())
            {
                var tasks = new List<Task<RecipientsInfo>>();
                int index = 1;
                foreach (var teamId in notification.Rosters)
                {
                    var task = context.CallActivityWithRetryAsync<RecipientsInfo>(
                                            FunctionNames.SyncTeamMembersActivity,
                                            FunctionSettings.DefaultRetryOptions,
                                            (notification.Id, teamId, index));
                    tasks.Add(task);
                    index++;
                }

                // Fan-Out Fan-In.
                await Task.WhenAll(tasks);
                var recipientsInfo = new RecipientsInfo();
                recipientsInfo.BatchName = new List<string>();
                foreach (var completedActivity in tasks)
                {
                    recipientsInfo.TotalRecipientCount += completedActivity.Result.TotalRecipientCount;
                    recipientsInfo.BatchName.AddRange(completedActivity.Result.BatchName);
                }

                return recipientsInfo;
            }

            // Members of M365 groups, DG or SG.
            if (notification.Groups.Any())
            {
                var tasks = new List<Task<RecipientsInfo>>();
                int index = 1;
                foreach (var groupId in notification.Groups)
                {
                    var task = context.CallActivityWithRetryAsync<RecipientsInfo>(
                                            FunctionNames.SyncGroupMembersActivity,
                                            FunctionSettings.DefaultRetryOptions,
                                            (notification.Id, groupId, index));

                    tasks.Add(task);
                    index++;
                }

                // Fan-Out Fan-In
                await Task.WhenAll(tasks);
                var recipientsInfo = new RecipientsInfo();
                recipientsInfo.BatchName = new List<string>();
                foreach (var completedActivity in tasks)
                {
                    recipientsInfo.TotalRecipientCount += completedActivity.Result.TotalRecipientCount;
                    recipientsInfo.BatchName.AddRange(completedActivity.Result.BatchName);
                }

                return recipientsInfo;
            }

            // General channel of teams.
            if (notification.Teams.Any())
            {
                return await context.CallActivityWithRetryAsync<RecipientsInfo>(
                    FunctionNames.SyncTeamsActivity,
                    FunctionSettings.DefaultRetryOptions,
                    notification);
            }

            // Invalid audience.
            var errorMessage = $"Invalid audience select for notification id: {notification.Id}";
            log.LogError(errorMessage);
            throw new ArgumentException(errorMessage);
        }
    }
}