// <copyright file="SendQueueOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// Send Queue orchestrator.
    ///
    /// Does following:
    /// 1. Reads all the recipients from Sent notification tables.
    /// 2. Starts data aggregation.
    /// 3. Sends messages to Send Queue in batches.
    /// </summary>
    public static class SendQueueOrchestrator
    {
        /// <summary>
        /// SendQueueOrchestrator function.
        /// Does following:
        /// 1. Reads all the recipients from Sent notification tables.
        /// 2. Starts data aggregation.
        /// 3. Sends messages to Send Queue in batches.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SendQueueOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var notification = context.GetInput<NotificationDataEntity>();

            // Update notification status.
            await context.CallActivityWithRetryAsync(
                FunctionNames.UpdateNotificationStatusActivity,
                FunctionSettings.DefaultRetryOptions,
                (notification.Id, NotificationStatus.Sending));

            if (!context.IsReplaying)
            {
                log.LogInformation("About to get all recipients.");
            }

            var recipients = await context.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(
                FunctionNames.GetRecipientsActivity,
                FunctionSettings.DefaultRetryOptions,
                notification);

            var recipientsList = recipients.ToList();

            if (!context.IsReplaying)
            {
                log.LogInformation("About to send data aggregration message to data queue.");
            }

            await context.CallActivityWithRetryAsync(
                FunctionNames.DataAggregationTriggerActivity,
                FunctionSettings.DefaultRetryOptions,
                (notification.Id, recipientsList.Count));

            var batches = SeparateIntoBatches(recipientsList);

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
                    (notification, batches[batchIndex]));

                tasks.Add(task);
            }

            // Fan-out Fan-in
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Separate a list of recipients into batches (a list of lists).
        /// The size of the batch is determined by the maximum allowed size of a batch
        /// request to the Send queue service bus queue.
        /// </summary>
        /// <param name="sourceList">The list to break into batches.</param>
        /// <returns>The batches (a list of lists).</returns>
        private static List<List<SentNotificationDataEntity>> SeparateIntoBatches(List<SentNotificationDataEntity> sourceList)
        {
            var batches = new List<List<SentNotificationDataEntity>>();

            var totalNumberOfEntities = sourceList.Count;

            // Use the SendQueue's maximum number of messages in a batch request number because
            // the list is being broken into batches in order to be added to that queue.
            var batchSize = SendQueue.MaxNumberOfMessagesInBatchRequest;
            var numberOfCompleteBatches = totalNumberOfEntities / batchSize;
            var numberOfEntitiesInIncompleteBatch = totalNumberOfEntities % batchSize;

            for (var i = 0; i < numberOfCompleteBatches; i++)
            {
                var startingIndex = i * batchSize;
                var batch = sourceList.GetRange(startingIndex, batchSize);
                batches.Add(batch);
            }

            if (numberOfEntitiesInIncompleteBatch != 0)
            {
                var incompleteBatchStartingIndex = numberOfCompleteBatches * batchSize;
                var incompleteBatch = sourceList.GetRange(
                    incompleteBatchStartingIndex,
                    numberOfEntitiesInIncompleteBatch);
                batches.Add(incompleteBatch);
            }

            return batches;
        }
    }
}