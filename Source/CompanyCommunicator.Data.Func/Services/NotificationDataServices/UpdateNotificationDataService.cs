// <copyright file="UpdateNotificationDataService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Newtonsoft.Json;

    /// <summary>
    /// Service to update notification data.
    /// </summary>
    public class UpdateNotificationDataService
    {
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateNotificationDataService"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public UpdateNotificationDataService(
            INotificationDataRepository notificationDataRepository,
            IHttpClientFactory httpClientFactory)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Updates the notification totals with the given information and results.
        /// </summary>
        /// <param name="notificationId">The notification ID.</param>
        /// <param name="orchestrationStatus">The orchestration status of the notification.</param>
        /// <param name="shouldForceCompleteNotification">Flag to indicate if the notification should
        /// be forced to be marked as completed.</param>
        /// <param name="totalExpectedNotificationCount">The total expected count of notifications to be sent.</param>
        /// <param name="aggregatedSentNotificationDataResults">The current aggregated results for
        /// the sent notifications.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<UpdateNotificationDataEntity> UpdateNotificationDataAsync(
            string notificationId,
            string orchestrationStatus,
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

                if (orchestrationStatus.Equals(nameof(OrchestrationStatus.Terminated), StringComparison.InvariantCultureIgnoreCase)
                   || orchestrationStatus.Equals(nameof(OrchestrationStatus.Completed), StringComparison.InvariantCultureIgnoreCase))
                {
                    if (currentTotalNotificationCount >= totalExpectedNotificationCount)
                    {
                        this.SetSentStatus(ref notificationDataEntityUpdate, lastSentDate);
                    }
                    else
                    {
                        var countDifference = totalExpectedNotificationCount - currentTotalNotificationCount;
                        this.SetCanceledStatus(ref notificationDataEntityUpdate, countDifference);
                    }
                }
                else

               // If it should be marked as complete, set the other values accordingly.
               if (currentTotalNotificationCount >= totalExpectedNotificationCount
                    || shouldForceCompleteNotification)
                {
                    if (currentTotalNotificationCount >= totalExpectedNotificationCount)
                    {
                        this.SetSentStatus(ref notificationDataEntityUpdate, lastSentDate);
                    }
                    else if (shouldForceCompleteNotification)
                    {
                        // If the message is being completed, not because all messages have been accounted for,
                        // but because the trigger is coming from the delayed Service Bus message that ensures that the
                        // notification will eventually be marked as complete, then update the unknown count of messages
                        // not accounted for and update the sent date to the current time.
                        var countDifference = totalExpectedNotificationCount - currentTotalNotificationCount;
                        this.SetSentStatusWithUnknownCount(ref notificationDataEntityUpdate, countDifference);
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

        /// <summary>
        /// Get the orchestration status of the notification.
        /// </summary>
        /// <param name="functionPayload">the payload of the orchestration containing Status Uri, Terminate Uri etc.</param>
        /// <returns>the status of the orchestration.</returns>
        public async Task<string> GetOrchestrationStatusAsync(string functionPayload)
        {
            var instancePayload = JsonConvert.DeserializeObject<HttpManagementPayload>(functionPayload);
            var client = this.httpClientFactory.CreateClient();
            var response = await client.GetAsync(instancePayload.StatusQueryGetUri);
            var content = await response.Content.ReadAsStringAsync();
            var functionResp = JsonConvert.DeserializeObject<OrchestrationStatusResponse>(content);
            return functionResp.RuntimeStatus;
        }

        private void SetSentStatus(ref UpdateNotificationDataEntity notificationDataEntityUpdate, DateTime? lastSentDate)
        {
            // Update the status to Sent.
            notificationDataEntityUpdate.Status = NotificationStatus.Sent.ToString();

            // If the message is being completed because all messages have been accounted for,
            // then make sure the unknown count is 0 and update the sent date with the date
            // of the last sent message.
            notificationDataEntityUpdate.Unknown = 0;
            notificationDataEntityUpdate.SentDate = lastSentDate ?? DateTime.UtcNow;
        }

        private void SetCanceledStatus(ref UpdateNotificationDataEntity notificationDataEntityUpdate, int canceledCount)
        {
            notificationDataEntityUpdate.Status = NotificationStatus.Canceled.ToString();

            // This count must stay 0 or above.
            canceledCount = canceledCount >= 0 ? canceledCount : 0;
            notificationDataEntityUpdate.Canceled = canceledCount;
            notificationDataEntityUpdate.SentDate = DateTime.UtcNow;
        }

        private void SetSentStatusWithUnknownCount(ref UpdateNotificationDataEntity notificationDataEntityUpdate, int unknownCount)
        {
            notificationDataEntityUpdate.Status = NotificationStatus.Sent.ToString();

            // This count must stay 0 or above.
            unknownCount = unknownCount >= 0 ? unknownCount : 0;
            notificationDataEntityUpdate.Unknown = unknownCount;
            notificationDataEntityUpdate.SentDate = DateTime.UtcNow;
        }
    }
}
