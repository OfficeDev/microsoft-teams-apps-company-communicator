// <copyright file="NotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository of the notification data in the table storage.
    /// </summary>
    public class NotificationRepository : BaseRepository<NotificationEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public NotificationRepository(IConfiguration configuration)
            : base(configuration, "Notification", PartitionKeyNames.Notification.DraftNotifications)
        {
        }

        /// <summary>
        /// Get all draft notification entities from the table storage.
        /// </summary>
        /// <returns>All draft notitification entities.</returns>
        public async Task<IEnumerable<NotificationEntity>> GetAllDraftNotificationsAsync()
        {
            var result = await this.GetAllAsync(PartitionKeyNames.Notification.DraftNotifications);

            return result;
        }

        /// <summary>
        /// Get the top 25 most recently sent notification entities from the table storage.
        /// </summary>
        /// <returns>The top 25 most recently sent notitification entities.</returns>
        public async Task<IEnumerable<NotificationEntity>> GetMostRecentSentNotifications()
        {
            var result = await this.GetAllAsync(PartitionKeyNames.Notification.SentNotifications);

            return result;
        }

        /// <summary>
        /// Move a draft notification from draft to sent partition.
        /// </summary>
        /// <param name="draftNotificationEntity">The draft notification instance to be moved to the sent partition.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task MoveDraftToSentPartition(NotificationEntity draftNotificationEntity)
        {
            if (draftNotificationEntity == null)
            {
                return;
            }

            // Create a sent notification based on the draft notification.
            var sentNotificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.SentNotifications,
                RowKey = draftNotificationEntity.RowKey,
                Id = draftNotificationEntity.Id,
                Title = draftNotificationEntity.Title,
                ImageLink = draftNotificationEntity.ImageLink,
                Summary = draftNotificationEntity.Summary,
                Author = draftNotificationEntity.Author,
                ButtonTitle = draftNotificationEntity.ButtonTitle,
                ButtonLink = draftNotificationEntity.ButtonLink,
                CreatedBy = draftNotificationEntity.CreatedBy,
                CreatedDate = draftNotificationEntity.CreatedDate,
                SentDate = DateTime.UtcNow.ToShortDateString(),
                IsDraft = false,
                Teams = draftNotificationEntity.Teams,
                Rosters = draftNotificationEntity.Rosters,
                AllUsers = draftNotificationEntity.AllUsers,
            };
            await this.CreateOrUpdateAsync(sentNotificationEntity);

            // Delete the draft notification.
            await this.DeleteAsync(draftNotificationEntity);
        }
    }
}
