// <copyright file="NotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Respository of the notification data.
    /// </summary>
    public class NotificationRepository : BaseRepository<NotificationEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public NotificationRepository(IConfiguration configuration)
            : base(configuration, "Notification")
        {
        }

        /// <summary>
        /// Get all notification entities from the table storage.
        /// </summary>
        /// <param name="isDraft">Indicates if the function shall return draft notifications or not.</param>
        /// <returns>All notitification entities.</returns>
        public async Task<IEnumerable<NotificationEntity>> All(bool isDraft)
        {
            var filter = TableQuery.GenerateFilterConditionForBool(
                    nameof(NotificationEntity.IsDraft),
                    QueryComparisons.Equal,
                    isDraft);

            var entities = await this.All(filter);

            return entities.Take(25);
        }

        /// <summary>
        /// Create a new draft notification.
        /// </summary>
        /// <param name="notification">Draft Notification model class instance passed in from Web API.</param>
        /// <param name="userName">Name of the user who is running the application.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task CreateDraftNotification(DraftNotification notification, string userName)
        {
            var id = Guid.NewGuid().ToString();
            var notificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.DraftNotifications,
                RowKey = id,
                Id = id,
                Title = notification.Title,
                ImageLink = notification.ImageLink,
                Summary = notification.Summary,
                Author = notification.Author,
                ButtonTitle = notification.ButtonTitle,
                ButtonLink = notification.ButtonLink,
                CreatedBy = userName,
                CreatedDate = DateTime.UtcNow.ToShortDateString(),
                IsDraft = true,
                Teams = notification.Teams,
                Rosters = notification.Rosters,
                AllUsers = notification.AllUsers,
            };

            await this.CreateOrUpdate(notificationEntity);
        }
    }
}
