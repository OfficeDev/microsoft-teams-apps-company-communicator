// <copyright file="INotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface to implement the notification data repository.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Get all notification entities from the table storage.
        /// </summary>
        /// <param name="isDraft">Indicates if the function shall return draft notifications or not.</param>
        /// <returns>All notitification entities.</returns>
        IEnumerable<NotificationEntity> All(bool isDraft);

        /// <summary>
        /// Create or update a notification entity in the table storage.
        /// </summary>
        /// <param name="entity"><see cref="NotificationEntity"/> instance.</param>
        void CreateOrUpdate(NotificationEntity entity);

        /// <summary>
        /// Delete a notification instance.
        /// </summary>
        /// <param name="entity"><see cref="NotificationEntity"/> instance.</param>
        void Delete(NotificationEntity entity);

        /// <summary>
        /// Get a specific notification entity in the table storage.
        /// </summary>
        /// <param name="partitionKey">The partition key of the notification entity.</param>
        /// <param name="rowKey">The row key fo the notification entity.</param>
        /// <returns>The <see cref="NotificationEntity"/> instance matching the keys.</returns>
        NotificationEntity Get(string partitionKey, string rowKey);
    }
}
