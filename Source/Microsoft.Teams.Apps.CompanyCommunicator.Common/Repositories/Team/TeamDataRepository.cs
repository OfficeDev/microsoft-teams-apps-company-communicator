// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository of the team data stored in the table storage.
    /// </summary>
    public class TeamDataRepository : BaseRepository<TeamDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public TeamDataRepository(IConfiguration configuration)
            : base(configuration, "TeamData", PartitionKeyNames.Metadata.TeamData)
        {
        }
    }
}
