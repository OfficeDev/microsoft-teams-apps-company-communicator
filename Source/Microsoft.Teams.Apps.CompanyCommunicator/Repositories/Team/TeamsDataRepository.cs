// <copyright file="TeamsDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification;

    /// <summary>
    /// Respository for the team data stored in the table storage.
    /// </summary>
    public class TeamsDataRepository : BaseRepository<TeamsDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public TeamsDataRepository(IConfiguration configuration)
            : base(configuration, "TeamsData")
        {
        }
    }
}
