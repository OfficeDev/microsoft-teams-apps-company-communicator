// <copyright file="SendQueueOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Orchestrators
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// Send Queue orchestrator.
    /// </summary>
    public static class SendQueueOrchestrator
    {
        /// <summary>
        /// Sends message recipients information in batches to <see cref="SendQueue.QueueName"/>.
        ///
        /// It uses Fan-out Fan-in pattern to process the batches in parallel.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SendQueueOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var dto = context.GetInput<SendMessageDTO>();

            var totalBatchCount = dto.TotalBatchCount;
            var notificationId = dto.NotificationId;

            if (!context.IsReplaying)
            {
                log.LogInformation($"About to process {totalBatchCount} batches.");
            }

            var tasks = new List<Task>();
            for (var batchIndex = 1; batchIndex <= totalBatchCount; batchIndex++)
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation($"About to process batch {batchIndex} / {totalBatchCount}");
                }

                var task = context.CallActivityWithRetryAsync(
                    FunctionNames.SendBatchMessagesActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationId, totalBatchCount));

                tasks.Add(task);
            }

            // Fan-out Fan-in
            await Task.WhenAll(tasks);
        }
    }
}