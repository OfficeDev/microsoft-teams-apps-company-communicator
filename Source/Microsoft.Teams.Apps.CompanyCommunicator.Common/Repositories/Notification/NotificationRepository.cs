// <copyright file="NotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;

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
        public async Task<IEnumerable<NotificationEntity>> GetAllAsync(bool isDraft)
        {
            var filter = TableQuery.GenerateFilterConditionForBool(
                    nameof(NotificationEntity.IsDraft),
                    QueryComparisons.Equal,
                    isDraft);

            var entities = await this.GetAllAsync(filter);

            return entities.Take(25);
        }
    }
}
