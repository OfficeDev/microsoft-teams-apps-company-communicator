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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;

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
                return await FanOutFanInActivityAsync(context, FunctionNames.SyncTeamMembersActivity, notification.Rosters,  notification.Id);
            }

            // Members of M365 groups, DG or SG.
            if (notification.Groups.Any())
            {
                return await FanOutFanInActivityAsync(context, FunctionNames.SyncGroupMembersActivity, notification.Groups,  notification.Id);
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

        /// <summary>
        /// Fan out Fan in activities.
        /// </summary>
        /// <param name="context">durable orchestration context.</param>
        /// <param name="functionName">activity name.</param>
        /// <param name="entities">entities e.g. groups or teams.</param>
        /// <param name="notificationId">notification id.</param>
        /// <returns>recipient information.</returns>
        private static async Task<RecipientsInfo> FanOutFanInActivityAsync(IDurableOrchestrationContext context, string functionName, IEnumerable<string> entities, string notificationId)
        {
            var tasks = new List<Task>();
            int index = 1;

            // Fan-out
            foreach (var entityId in entities)
            {
                var task = context.CallActivityWithRetryAsync(
                                        functionName,
                                        FunctionSettings.DefaultRetryOptions,
                                        (notificationId, entityId, index));

                tasks.Add(task);
                index++;
            }

            // Fan-In
            await Task.WhenAll(tasks);

            // Batch recipients.
            return await context.CallActivityWithRetryAsync<RecipientsInfo>(
                  FunctionNames.BatchRecipientsActivity,
                  FunctionSettings.DefaultRetryOptions,
                  notificationId);
        }
    }
}