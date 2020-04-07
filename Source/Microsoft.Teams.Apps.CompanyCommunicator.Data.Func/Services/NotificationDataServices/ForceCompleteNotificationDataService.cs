// <copyright file="ForceCompleteNotificationDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// A service that forces a notification data entity to be set as completed.
    /// </summary>
    public class ForceCompleteNotificationDataService
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForceCompleteNotificationDataService"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        public ForceCompleteNotificationDataService(NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Forces a notification data entity to be set as completed.
        /// </summary>
        /// <param name="notificationDataEntity">The notification data entity to be set as completed.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ForceCompleteAsync(NotificationDataEntity notificationDataEntity)
        {
            // If the notification is already marked complete, then nothing needs to be done.
            if (!notificationDataEntity.IsCompleted)
            {
                var incompleteTotalMessageCount = notificationDataEntity.Succeeded
                    + notificationDataEntity.Throttled
                    + notificationDataEntity.Failed;

                var unknownCount = notificationDataEntity.TotalMessageCount - incompleteTotalMessageCount;

                var forceCompletedNotificationDataEntityUpdate = new UpdateNotificationDataEntity
                {
                    PartitionKey = NotificationDataTableNames.SentNotificationsPartition,
                    RowKey = notificationDataEntity.RowKey,
                    Unknown = unknownCount,
                    IsCompleted = true,
                    SentDate = DateTime.UtcNow,
                };

                var operation = TableOperation.InsertOrMerge(forceCompletedNotificationDataEntityUpdate);
                await this.notificationDataRepository.Table.ExecuteAsync(operation);
            }
        }
    }
}
