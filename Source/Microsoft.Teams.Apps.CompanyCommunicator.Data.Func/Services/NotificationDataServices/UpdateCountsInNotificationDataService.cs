// <copyright file="UpdateCountsInNotificationDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;

    /// <summary>
    /// The service to update the counts in a notification data entity.
    /// </summary>
    public class UpdateCountsInNotificationDataService
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateCountsInNotificationDataService"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        public UpdateCountsInNotificationDataService(NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Updates the counts for a notification data entity.
        /// </summary>
        /// <param name="notificationDataEntity">The notification data entity whose counts should be updated.</param>
        /// <param name="resultTypeToAdd">The result type to add to the counts.</param>
        /// <param name="resultSentDate">The sent date for that result.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateCountsAsync(
            NotificationDataEntity notificationDataEntity,
            DataQueueResultType resultTypeToAdd,
            DateTime? resultSentDate)
        {
            var succeededCount = notificationDataEntity.Succeeded;
            var throttledCount = notificationDataEntity.Throttled;
            var failedCount = notificationDataEntity.Failed;

            if (resultTypeToAdd == DataQueueResultType.Succeeded)
            {
                succeededCount++;
            }
            else if (resultTypeToAdd == DataQueueResultType.Throttled)
            {
                throttledCount++;
            }
            else
            {
                failedCount++;
            }

            // Purposefully exclude the unknown count because those messages may be sent later
            var currentTotalMessageCount = succeededCount
                + throttledCount
                + failedCount;

            var notificationDataEntityUpdate = new UpdateNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                RowKey = notificationDataEntity.RowKey,
                Succeeded = succeededCount,
                Failed = failedCount,
                Throttled = throttledCount,
            };

            if (currentTotalMessageCount >= notificationDataEntity.TotalMessageCount)
            {
                notificationDataEntityUpdate.IsCompleted = true;
                notificationDataEntityUpdate.SentDate = resultSentDate ?? DateTime.UtcNow;
            }

            var operation = TableOperation.InsertOrMerge(notificationDataEntityUpdate);
            await this.notificationDataRepository.Table.ExecuteAsync(operation);
        }
    }
}
