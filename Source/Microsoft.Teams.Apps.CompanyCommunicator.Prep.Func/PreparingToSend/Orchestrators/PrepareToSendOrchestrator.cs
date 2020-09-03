// <copyright file="PrepareToSendOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Prepare to Send orchestrator.
    ///
    /// This function prepares to send a notification to the target audience.
    ///
    /// Performs following:
    /// 1. Fetch and store recipients information and batch the recipients for further processing.
    /// 2. Prepare and store the message to be sent in notification table.
    /// 3. Update notification metadata.
    /// 4. Send a message to Data Queue to start aggregating data.
    /// 5. Send a batch of queue messages (1 message for each recipient) to send queue.
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
        [FunctionName(nameof(PrepareToSendOrchestrationAsync))]
        public static async Task PrepareToSendOrchestrationAsync(
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
                    log.LogInformation("About to process recipient data.");
                }

                var recipientsInfo = await context.CallSubOrchestratorWithRetryAsync<RecipientDataListInformation>(
                    FunctionNames.ProcessRecipientsOrchestrator,
                    FunctionSettings.DefaultRetryOptions,
                    notificationDataEntity);

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to process and store message.");
                }

                await context.CallActivityWithRetryAsync(
                    FunctionNames.PrepareAndStoreMessageActivity,
                    FunctionSettings.DefaultRetryOptions,
                    notificationDataEntity);

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to update notification entity.");
                }

                await context.CallActivityWithRetryAsync(
                    FunctionNames.UpdateNotificationActivity,
                    FunctionSettings.DefaultRetryOptions,
                    new NotificationMetadataDTO
                    {
                        NotificationId = notificationDataEntity.Id,
                        TotalNumberOfRecipients = recipientsInfo.TotalNumberOfRecipients,
                    });

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to send data aggregration message to data queue.");
                }

                await context.CallActivityWithRetryAsync(
                    FunctionNames.DataAggregationActivity,
                    FunctionSettings.DefaultRetryOptions,
                    notificationDataEntity.Id);

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to send batch queue messages to send queue.");
                }

                await context.CallSubOrchestratorWithRetryAsync(
                    FunctionNames.SendQueueOrchestrator,
                    FunctionSettings.DefaultRetryOptions,
                    new SendMessageDTO
                    {
                        NotificationId = notificationDataEntity.Id,
                        TotalBatchCount = recipientsInfo.NumberOfRecipientDataBatches,
                    });

                log.LogInformation($"PrepareToSendOrchestrator successfully completed for notification: {notificationDataEntity.Id}!");
            }
            catch (Exception ex)
            {
                var errorMessage = $"PrepareToSendOrchestrator failed for notification: {notificationDataEntity.Id}. Exception Message: {ex.Message}";
                log.LogError(ex, errorMessage);

                await context.CallActivityWithRetryAsync(
                    FunctionNames.HandleFailureActivity,
                    FunctionSettings.DefaultRetryOptions,
                    new HandleFailureActivityDTO
                    {
                        NotificationDataEntity = notificationDataEntity,
                        Exception = ex,
                    });
            }
        }
    }
}
