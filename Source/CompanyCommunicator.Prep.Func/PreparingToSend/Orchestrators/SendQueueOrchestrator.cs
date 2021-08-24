// <copyright file="SendQueueOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Send Queue orchestrator.
    ///
    /// Does following:
    /// 1. Update the status in message activity.
    /// 2. Starts data aggregation.
    /// 3. Fan-out/Fan-in sub orchestration.
    /// </summary>
    public static class SendQueueOrchestrator
    {
        /// <summary>
        /// SendQueueOrchestrator function.
        /// Does following:
        /// 1. Update the status in message activity.
        /// 2. Starts data aggregation.
        /// 3. Fan-out/Fan-in sub orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SendQueueOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var recipientsInfo = context.GetInput<RecipientsInfo>();

            // Update notification status.
            await context.CallActivityWithRetryAsync(
                FunctionNames.UpdateNotificationStatusActivity,
                FunctionSettings.DefaultRetryOptions,
                (recipientsInfo.NotificationId, NotificationStatus.Sending));

            await context.CallActivityWithRetryAsync(
                FunctionNames.DataAggregationTriggerActivity,
                FunctionSettings.DefaultRetryOptions,
                (recipientsInfo.NotificationId, recipientsInfo.TotalRecipientCount));

            var tasks = new List<Task>();
            foreach (var batchKey in recipientsInfo.BatchName)
            {
                var task = context.CallSubOrchestratorWithRetryAsync(
                    FunctionNames.SendQueueSubOrchestrator,
                    FunctionSettings.DefaultRetryOptions,
                    batchKey);
                tasks.Add(task);
            }

            // Fan-out Fan-in
            await Task.WhenAll(tasks);
        }
    }
}