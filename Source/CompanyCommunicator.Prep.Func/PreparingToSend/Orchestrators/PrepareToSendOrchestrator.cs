// <copyright file="PrepareToSendOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;

    /// <summary>
    /// Prepare to Send orchestrator.
    ///
    /// This function prepares to send a notification to the target audience.
    ///
    /// Performs following:
    /// 1. Stores the message in sending notification table.
    /// 2. Syncs recipients information to sent notification table.
    /// 3. Creates teams conversation with recipients if required.
    /// 4. Starts Data aggregation.
    /// 5. Starts Send Queue orchestration.
    /// </summary>
    public static class PrepareToSendOrchestrator
    {
        /// <summary>
        /// This is the durable orchestration method,
        /// which kicks off the preparing to send process.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(FunctionNames.PrepareToSendOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            if (!context.IsReplaying)
            {
                log.LogInformation($"Start to prepare to send the notification {notificationDataEntity.Id}!");
            }

            try
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation("About to store message content.");
                }

                await context.CallActivityWithRetryAsync(
                    FunctionNames.StoreMessageActivity,
                    FunctionSettings.DefaultRetryOptions,
                    notificationDataEntity);

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to sync recipients.");
                }

                var recipientsInfo = await context.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(
                    FunctionNames.SyncRecipientsOrchestrator,
                    FunctionSettings.DefaultRetryOptions,
                    notificationDataEntity);

                // Proactive Installation
                if (recipientsInfo.HasRecipientsPendingInstallation)
                {
                    if (!context.IsReplaying)
                    {
                        log.LogInformation("About to create 1:1 conversations for recipients if required.");
                    }

                    // Update notification status.
                    await context.CallActivityWithRetryAsync(
                        FunctionNames.UpdateNotificationStatusActivity,
                        FunctionSettings.DefaultRetryOptions,
                        (recipientsInfo.NotificationId, NotificationStatus.InstallingApp));

                    // Fan Out/Fan In Conversation orchestrator.
                    await FanOutFanInSubOrchestratorAsync(context, FunctionNames.TeamsConversationOrchestrator, recipientsInfo);
                }

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to send messages to send queue.");
                }

                // Update notification status.
                await context.CallActivityWithRetryAsync(
                    FunctionNames.UpdateNotificationStatusActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationDataEntity.Id, NotificationStatus.Sending));

                // Update Total recipient count.
                await context.CallActivityWithRetryAsync(
                    FunctionNames.DataAggregationTriggerActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationDataEntity.Id, recipientsInfo.TotalRecipientCount));

                // Fan-out/ Fan-in send queue orchestrator.
                await FanOutFanInSubOrchestratorAsync(context, FunctionNames.SendQueueOrchestrator, recipientsInfo);

                log.LogInformation($"PrepareToSendOrchestrator successfully completed for notification: {notificationDataEntity.Id}!");
            }
            catch (Exception ex)
            {
                var errorMessage = $"PrepareToSendOrchestrator failed for notification: {notificationDataEntity.Id}. Exception Message: {ex.Message}";
                log.LogError(ex, errorMessage);

                await context.CallActivityWithRetryAsync(
                    FunctionNames.HandleFailureActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationDataEntity, ex));
            }
        }

        private static async Task FanOutFanInSubOrchestratorAsync(IDurableOrchestrationContext context, string functionName, RecipientsInfo recipientsInfo)
        {
            var tasks = new List<Task>();

            // Fan-out
            foreach (var batchKey in recipientsInfo.BatchKeys)
            {
                var task = context.CallSubOrchestratorWithRetryAsync(
                    functionName,
                    FunctionSettings.DefaultRetryOptions,
                    batchKey);
                tasks.Add(task);
            }

            // Fan-in
            await Task.WhenAll(tasks);
        }
    }
}
