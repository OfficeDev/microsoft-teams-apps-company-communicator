// <copyright file="NotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification
{
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository for the notification data in the table storage.
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
        public IEnumerable<NotificationEntity> All(bool isDraft)
        {
            var filter = TableQuery.GenerateFilterConditionForBool(
                    nameof(NotificationEntity.IsDraft),
                    QueryComparisons.Equal,
                    isDraft);

            var entities = this.All(filter);

            return entities;
        }
    }
}
