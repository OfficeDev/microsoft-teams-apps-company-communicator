// <copyright file="ActiveNotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.ActiveNotification
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository for the active notification data.
    /// </summary>
    public class ActiveNotificationRepository : BaseRepository<ActiveNotificationEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveNotificationRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public ActiveNotificationRepository(IConfiguration configuration)
            : base(configuration, "ActiveNotification")
        {
        }
    }
}