// <copyright file="TeamsConversationOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Utilities;

    /// <summary>
    /// Teams conversation orchestrator.
    /// Does following:
    /// 1. Gets the batch recipients for whom we do not have conversation Id.
    /// 2. Creates conversation with each recipient.
    /// </summary>
    public static class TeamsConversationOrchestrator
    {
        /// <summary>
        /// TeamsConversationOrchestrator function.
        /// Does following:
        /// 1. Gets all the pending recipients(for whom we do not have conversation Id).
        /// 2. Creates conversation with each recipient.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.TeamsConversationOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var batchPartitionKey = context.GetInput<string>();
            var notificationId = PartitionKeyUtility.GetNotificationIdFromBatchPartitionKey(batchPartitionKey);

            if (!context.IsReplaying)
            {
                log.LogInformation($"About to get pending recipients (with no conversation id in database).");
            }

            var recipients = await context.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(
            FunctionNames.GetPendingRecipientsActivity,
            FunctionSettings.DefaultRetryOptions,
            batchPartitionKey);

            var count = recipients.ToList().Count;
            if (count == 0)
            {
                log.LogInformation("No pending recipients.");
                return;
            }

            if (!context.IsReplaying)
            {
                log.LogInformation($"About to create 1:1 conversations with {count} recipients.");
            }

            // Create conversation.
            var tasks = new List<Task>();
            foreach (var recipient in recipients)
            {
                // Update batch partition key to actual notification Id.
                // Because batch partition key is used only for batching data.
                // Actual state and data is stored against the notification id record in SentNotificationData Table.
                recipient.PartitionKey = notificationId;

                var task = context.CallActivityWithRetryAsync(
                    FunctionNames.TeamsConversationActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationId, batchPartitionKey, recipient));
                tasks.Add(task);
            }

            // Fan-out Fan-in.
            await Task.WhenAll(tasks);
        }
    }
}
