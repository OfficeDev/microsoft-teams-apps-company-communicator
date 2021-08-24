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

    /// <summary>
    /// Teams conversation orchestrator.
    /// Does following:
    /// 1. Gets all the recipients for whom we do not have conversation Id.
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
            var partitionKey = context.GetInput<string>();
            var notificationId = partitionKey.Split(':')[0];

            if (!context.IsReplaying)
            {
                log.LogInformation($"About to get pending recipients (with no conversation id in database.");
            }

            var recipients = await context.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(
            FunctionNames.GetPendingRecipientsActivity,
            FunctionSettings.DefaultRetryOptions,
            partitionKey);

            var count = recipients.Count();
            if (count < 0)
            {
                log.LogInformation("No pending recipients.");
                return;
            }

            if (!context.IsReplaying)
            {
                log.LogInformation($"About to create conversation with {count} recipients.");
            }

            // Create conversation.
            var tasks = new List<Task>();
            foreach (var recipient in recipients)
            {
                // Update partition key to actual notification Id.
                recipient.PartitionKey = notificationId;
                var task = context.CallActivityWithRetryAsync(
                    FunctionNames.TeamsConversationActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationId, partitionKey, recipient));
                tasks.Add(task);
            }

            // Fan-out Fan-in.
            await Task.WhenAll(tasks);
        }
    }
}
