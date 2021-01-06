// <copyright file="UpdateNotificationDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Service to update notification data.
    /// </summary>
    public class UpdateNotificationDataService
    {
        private readonly INotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateNotificationDataService"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        public UpdateNotificationDataService(
            INotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Updates the notification totals with the given information and results.
        /// </summary>
        /// <param name="notificationId">The notification ID.</param>
        /// <param name="shouldForceCompleteNotification">Flag to indicate if the notification should
        /// be forced to be marked as completed.</param>
        /// <param name="totalExpectedNotificationCount">The total expected count of notifications to be sent.</param>
        /// <param name="aggregatedSentNotificationDataResults">The current aggregated results for
        /// the sent notifications.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<UpdateNotificationDataEntity> UpdateNotificationDataAsync(
            string notificationId,
            bool shouldForceCompleteNotification,
            int totalExpectedNotificationCount,
            AggregatedSentNotificationDataResults aggregatedSentNotificationDataResults,
            ILogger log)
        {
            try
            {
                var currentTotalNotificationCount = aggregatedSentNotificationDataResults.CurrentTotalNotificationCount;
                var succeededCount = aggregatedSentNotificationDataResults.SucceededCount;
                var failedCount = aggregatedSentNotificationDataResults.FailedCount;
                var throttledCount = aggregatedSentNotificationDataResults.ThrottledCount;
                var recipientNotFoundCount = aggregatedSentNotificationDataResults.RecipientNotFoundCount;
                var lastSentDate = aggregatedSentNotificationDataResults.LastSentDate;

                // Create the general update.
                var notificationDataEntityUpdate = new UpdateNotificationDataEntity
                {
                    PartitionKey = NotificationDataTableNames.SentNotificationsPartition,
                    RowKey = notificationId,
                    Succeeded = succeededCount,
                    Failed = failedCount,
                    RecipientNotFound = recipientNotFoundCount,
                    Throttled = throttledCount,
                };

                // If it should be marked as complete, set the other values accordingly.
                if (currentTotalNotificationCount >= totalExpectedNotificationCount
                    || shouldForceCompleteNotification)
                {
                    // Update the status to Sent.
                    notificationDataEntityUpdate.Status = NotificationStatus.Sent.ToString();

                    if (currentTotalNotificationCount >= totalExpectedNotificationCount)
                    {
                        // If the message is being completed because all messages have been accounted for,
                        // then make sure the unknown count is 0 and update the sent date with the date
                        // of the last sent message.
                        notificationDataEntityUpdate.Unknown = 0;
                        notificationDataEntityUpdate.SentDate = lastSentDate ?? DateTime.UtcNow;
                    }
                    else if (shouldForceCompleteNotification)
                    {
                        // If the message is being completed, not because all messages have been accounted for,
                        // but because the trigger is coming from the delayed Service Bus message that ensures that the
                        // notification will eventually be marked as complete, then update the unknown count of messages
                        // not accounted for and update the sent date to the current time.
                        var countDifference = totalExpectedNotificationCount - currentTotalNotificationCount;

                        // This count must stay 0 or above.
                        var unknownCount = countDifference >= 0 ? countDifference : 0;

                        notificationDataEntityUpdate.Unknown = unknownCount;
                        notificationDataEntityUpdate.SentDate = DateTime.UtcNow;
                    }
                }

                var operation = TableOperation.InsertOrMerge(notificationDataEntityUpdate);
                await this.notificationDataRepository.Table.ExecuteAsync(operation);

                return notificationDataEntityUpdate;
            }
            catch (Exception e)
            {
                var errorMessage = $"{e.GetType()}: {e.Message}";
                log.LogError(e, $"ERROR: {errorMessage}");
                throw;
            }
        }
    }
}
