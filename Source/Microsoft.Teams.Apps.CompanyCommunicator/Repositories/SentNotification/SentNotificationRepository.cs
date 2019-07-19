// <copyright file="SentNotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.SentNotification
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository of the notification data.
    /// </summary>
    public class SentNotificationRepository : BaseRepository<SentNotificationEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public SentNotificationRepository(IConfiguration configuration)
            : base(configuration, "SentNotification")
        {
        }
    }
}