// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
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

            return teamDataEntities.Select(p => p.Name);
        }
    }
}
