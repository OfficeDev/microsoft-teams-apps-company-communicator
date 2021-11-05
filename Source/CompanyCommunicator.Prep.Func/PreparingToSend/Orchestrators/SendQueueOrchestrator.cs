// <copyright file="SendQueueOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Orchestrators
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Utilities;

    /// <summary>
    /// Send Queue orchestrator.
    ///
    /// Does following:
    /// 1. Reads all the recipients from Sent notification tables.
    /// 2. Sends messages to Send Queue in batches.
    /// </summary>
    public static class SendQueueOrchestrator
    {
        /// <summary>
        /// SendQueueSubOrchestrator function.
        /// Does following:
        /// 1. Reads the batch recipients from Sent notification tables.
        /// 2. Sends messages to Send Queue in batches.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SendQueueOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var batchPartitionKey = context.GetInput<string>();
            var notificationId = PartitionKeyUtility.GetNotificationIdFromBatchPartitionKey(batchPartitionKey);
            var batchId = PartitionKeyUtility.GetBatchIdFromBatchPartitionKey(batchPartitionKey);

            if (!context.IsReplaying)
            {
                log.LogInformation($"About to get recipients from batch {batchId}.");
            }

            var recipients = await context.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(
                FunctionNames.GetRecipientsActivity,
                FunctionSettings.DefaultRetryOptions,
                batchPartitionKey);

            // Use the SendQueue's maximum number of messages in a batch request number because
            // the list is being broken into batches in order to be added to that queue.
            var batches = recipients.AsBatches(SendQueue.MaxNumberOfMessagesInBatchRequest).ToList();

            var totalBatchCount = batches.Count;
            if (!context.IsReplaying)
            {
                log.LogInformation($"About to process {totalBatchCount} batches.");
            }

            var tasks = new List<Task>();
            for (var batchIndex = 0; batchIndex < totalBatchCount; batchIndex++)
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation($"About to process batch {batchIndex + 1} / {totalBatchCount}");
                }

                var task = context.CallActivityWithRetryAsync(
                    FunctionNames.SendBatchMessagesActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationId, batches[batchIndex]));

                tasks.Add(task);
            }

            // Fan-out Fan-in
            await Task.WhenAll(tasks);
        }
    }
}
