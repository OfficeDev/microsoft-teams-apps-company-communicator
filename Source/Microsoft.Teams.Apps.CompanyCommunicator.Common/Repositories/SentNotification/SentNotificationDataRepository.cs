// <copyright file="SentNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotification
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository of the notification data.
    /// </summary>
    public class SentNotificationDataRepository : BaseRepository<SentNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public SentNotificationDataRepository(IConfiguration configuration)
            : base(configuration, "SentNotification")
        {
        }
    }
}