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
        /// Get all notification entities from the table storage.
        /// </summary>
        /// <param name="isDraft">Indicates if the function shall return draft notifications or not.</param>
        /// <returns>All notitification entities.</returns>
        public async Task<IEnumerable<NotificationEntity>> GetAllAsync(bool isDraft)
        {
            var partitionKey = isDraft ? PartitionKeyNames.Notification.DraftNotifications : PartitionKeyNames.Notification.SentNotifications;

            var result = await this.GetAllAsync(partitionKey);

            return result;
        }

        /// <summary>
        /// Move a draft notification from draft to sent partition.
        /// </summary>
        /// <param name="draftNotificationEntity">The draft notificatin instance to be moved to the sent partition.</param>
        /// <returns>Indicates if it moves the draft to sent partition successfully.</returns>
        public async Task<bool> MoveDraftToSentPartition(NotificationEntity draftNotificationEntity)
        {
            if (draftNotificationEntity == null)
            {
                return false;
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

            return true;
        }
    }
}
