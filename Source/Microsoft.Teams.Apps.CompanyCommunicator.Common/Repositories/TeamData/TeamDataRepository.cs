// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository of the team data stored in the table storage.
    /// </summary>
    public class TeamDataRepository : BaseRepository<TeamDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public TeamDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                  configuration,
                  PartitionKeyNames.TeamDataTable.TableName,
                  PartitionKeyNames.TeamDataTable.TeamDataPartition,
                  isFromAzureFunction)
        {
        }

        /// <summary>
        /// Gets team data entities by ID values.
        /// </summary>
        /// <param name="teamIds">Team IDs.</param>
        /// <returns>Team data entities.</returns>
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(IEnumerable<string> teamIds)
        {
            var rowKeysFilter = string.Empty;
            foreach (var teamId in teamIds)
            {
                var singleRowKeyFilter = TableQuery.GenerateFilterCondition(
                    nameof(TableEntity.RowKey),
                    QueryComparisons.Equal,
                    teamId);

                if (string.IsNullOrWhiteSpace(rowKeysFilter))
                {
                    rowKeysFilter = singleRowKeyFilter;
                }
                else
                {
                    rowKeysFilter = TableQuery.CombineFilters(rowKeysFilter, TableOperators.Or, singleRowKeyFilter);
                }
            }

            return await this.GetWithFilterAsync(rowKeysFilter);
        }

        /// <summary>
        /// Get team names by Ids.
        /// </summary>
        /// <param name="ids">Team ids.</param>
        /// <returns>Names of the teams matching incoming ids.</returns>
        public async Task<IEnumerable<string>> GetTeamNamesByIdsAsync(IEnumerable<string> ids)
        {
            if (ids == null || ids.Count() == 0)
            {
                return new List<string>();
            }

            var rowKeysFilter = string.Empty;
            foreach (var id in ids)
            {
                var singleRowKeyFilter = TableQuery.GenerateFilterCondition(
                    nameof(TableEntity.RowKey),
                    QueryComparisons.Equal,
                    id);

                if (string.IsNullOrWhiteSpace(rowKeysFilter))
                {
                    rowKeysFilter = singleRowKeyFilter;
                }
                else
                {
                    rowKeysFilter = TableQuery.CombineFilters(rowKeysFilter, TableOperators.Or, singleRowKeyFilter);
                }
            }

            var teamDataEntities = await this.GetWithFilterAsync(rowKeysFilter);

            return teamDataEntities.Select(p => p.Name).OrderBy(p => p);
        }

        /// <summary>
        /// Get all team data entities, and sort the result alphabetically by name.
        /// </summary>
        /// <returns>The team data entities sorted alphabetically by name.</returns>
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
