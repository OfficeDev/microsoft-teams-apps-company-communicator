// <copyright file="ITeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for Team Data Repository.
    /// </summary>
    public interface ITeamDataRepository : IRepository<TeamDataEntity>
    {
        /// <summary>
        /// Gets team data entities by ID values.
        /// </summary>
        /// <param name="teamIds">Team IDs.</param>
        /// <returns>Team data entities.</returns>
        public Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(IEnumerable<string> teamIds);

        /// <summary>
        /// Get team names by Ids.
        /// </summary>
        /// <param name="ids">Team ids.</param>
        /// <returns>Names of the teams matching incoming ids.</returns>
        public Task<IEnumerable<string>> GetTeamNamesByIdsAsync(IEnumerable<string> ids);

        /// <summary>
        /// Get all team data entities, and sort the result alphabetically by name.
        /// </summary>
        /// <returns>The team data entities sorted alphabetically by name.</returns>
        public Task<IEnumerable<TeamDataEntity>> GetAllSortedAlphabeticallyByNameAsync();
    }
}
