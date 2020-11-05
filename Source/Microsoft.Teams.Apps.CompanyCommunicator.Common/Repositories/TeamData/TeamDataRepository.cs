// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository of the team data stored in the table storage.
    /// </summary>
    public class TeamDataRepository : BaseRepository<TeamDataEntity>, ITeamDataRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public TeamDataRepository(
            ILogger<TeamDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: TeamDataTableNames.TableName,
                  defaultPartitionKey: TeamDataTableNames.TeamDataPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(IEnumerable<string> teamIds)
        {
            var rowKeysFilter = this.GetRowKeysFilter(teamIds);

            return await this.GetWithFilterAsync(rowKeysFilter);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetTeamNamesByIdsAsync(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<string>();
            }

            var rowKeysFilter = this.GetRowKeysFilter(ids);
            var teamDataEntities = await this.GetWithFilterAsync(rowKeysFilter);

            return teamDataEntities.Select(p => p.Name).OrderBy(p => p);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TeamDataEntity>> GetAllSortedAlphabeticallyByNameAsync()
        {
            var teamDataEntities = await this.GetAllAsync();
            var sortedSet = new SortedSet<TeamDataEntity>(teamDataEntities, new TeamDataEntityComparer());
            return sortedSet;
        }

        private class TeamDataEntityComparer : IComparer<TeamDataEntity>
        {
            public int Compare(TeamDataEntity x, TeamDataEntity y)
            {
                return x.Name.CompareTo(y.Name);
            }
        }
    }
}
