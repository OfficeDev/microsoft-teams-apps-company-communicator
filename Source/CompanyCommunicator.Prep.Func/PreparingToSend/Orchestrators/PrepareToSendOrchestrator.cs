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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Prepare to Send orchestrator.
    ///
    /// This function prepares to send a notification to the target audience.
    ///
    /// Performs following:
    /// 1. Stores the message in sending notification table.
    /// 2. Syncs recipients information to sent notification table.
    /// 3. Creates teams conversation with recipients if required.
    /// 4. Starts Send Queue orchestration.
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

                recipientsInfo.NotificationId = notificationDataEntity.Id;

                // Proactive Installation
                if (recipientsInfo.IsPendingRecipient)
                {
                    if (!context.IsReplaying)
                    {
                        log.LogInformation("About to create conversation for recipients if required.");
                    }

                    // Update notification status.
                    await context.CallActivityWithRetryAsync(
                        FunctionNames.UpdateNotificationStatusActivity,
                        FunctionSettings.DefaultRetryOptions,
                        (recipientsInfo.NotificationId, NotificationStatus.InstallingApp));

                    // Fan Out/Fan In Conversation orchestrator.
                    var tasks = new List<Task>();
                    foreach (var batchKey in recipientsInfo.BatchName)
                    {
                        var task = context.CallSubOrchestratorWithRetryAsync(
                            FunctionNames.TeamsConversationOrchestrator,
                            FunctionSettings.DefaultRetryOptions,
                            batchKey);
                        tasks.Add(task);
                    }

                    // Fan-out Fan-in.
                    await Task.WhenAll(tasks);
                }

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to send messages to send queue.");
                }

                await context.CallSubOrchestratorWithRetryAsync(
                    FunctionNames.SendQueueOrchestrator,
                    FunctionSettings.DefaultRetryOptions,
                    recipientsInfo);

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
    }
}
