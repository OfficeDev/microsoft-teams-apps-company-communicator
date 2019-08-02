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
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public NotificationRepository(IConfiguration configuration, TableRowKeyGenerator tableRowKeyGenerator)
            : base(configuration, "Notification", PartitionKeyNames.Notification.DraftNotifications)
        {
            this.TableRowKeyGenerator = tableRowKeyGenerator;
        }

        /// <summary>
        /// Gets table row key generator.
        /// </summary>
        public TableRowKeyGenerator TableRowKeyGenerator { get; }

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
        public async Task<IEnumerable<NotificationEntity>> GetMostRecentSentNotificationsAsync()
        {
            var result = await this.GetAllAsync(PartitionKeyNames.Notification.SentNotifications, 25);

            return result;
        }

        /// <summary>
        /// Move a draft notification from draft to sent partition.
        /// </summary>
        /// <param name="draftNotificationEntity">The draft notification instance to be moved to the sent partition.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task MoveDraftToSentPartitionAsync(NotificationEntity draftNotificationEntity)
        {
            if (draftNotificationEntity == null)
            {
                return;
            }

            var newId = this.TableRowKeyGenerator.CreateNewKeyOrderingMostRecentToOldest();

            // Create a sent notification based on the draft notification.
            var sentNotificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.SentNotifications,
                RowKey = newId,
                Id = newId,
                Title = draftNotificationEntity.Title,
                ImageLink = draftNotificationEntity.ImageLink,
                Summary = draftNotificationEntity.Summary,
                Author = draftNotificationEntity.Author,
                ButtonTitle = draftNotificationEntity.ButtonTitle,
                ButtonLink = draftNotificationEntity.ButtonLink,
                CreatedBy = draftNotificationEntity.CreatedBy,
                CreatedDateTime = draftNotificationEntity.CreatedDateTime,
                SentDate = null,
                IsDraft = false,
                Teams = draftNotificationEntity.Teams,
                Rosters = draftNotificationEntity.Rosters,
                AllUsers = draftNotificationEntity.AllUsers,
            };
            await this.CreateOrUpdateAsync(sentNotificationEntity);

            // Delete the draft notification.
            await this.DeleteAsync(draftNotificationEntity);
        }

        /// <summary>
        /// Duplicate an existing draft notification.
        /// </summary>
        /// <param name="notificationEntity">The notification entity to be duplicated.</param>
        /// <param name="createdBy">Created by.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task DuplicateDraftNotificationAsync(
            NotificationEntity notificationEntity,
            string createdBy)
        {
            var newId = this.TableRowKeyGenerator.CreateNewKeyOrderingOldestToMostRecent();

            var newNotificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.DraftNotifications,
                RowKey = newId,
                Id = newId,
                Title = notificationEntity.Title,
                ImageLink = notificationEntity.ImageLink,
                Summary = notificationEntity.Summary,
                Author = notificationEntity.Author,
                ButtonTitle = notificationEntity.ButtonTitle,
                ButtonLink = notificationEntity.ButtonLink,
                CreatedBy = createdBy,
                CreatedDateTime = DateTime.UtcNow,
                IsDraft = true,
                Teams = notificationEntity.Teams,
                Rosters = notificationEntity.Rosters,
                AllUsers = notificationEntity.AllUsers,
            };

            await this.CreateOrUpdateAsync(newNotificationEntity);
        }
    }
}
