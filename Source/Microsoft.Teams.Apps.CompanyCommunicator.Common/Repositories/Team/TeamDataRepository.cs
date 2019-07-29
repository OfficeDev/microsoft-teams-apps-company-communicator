// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team
{
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

            var partitionKeyFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                PartitionKeyNames.Metadata.TeamData);

            var rowKeyFiter = string.Empty;
            foreach (var id in ids)
            {
                var subRowKeyFilter = TableQuery.GenerateFilterCondition(
                    nameof(TableEntity.RowKey),
                    QueryComparisons.Equal,
                    id.ToString());

                if (string.IsNullOrWhiteSpace(rowKeyFiter))
                {
                    rowKeyFiter = subRowKeyFilter;
                }
                else
                {
                    rowKeyFiter = TableQuery.CombineFilters(rowKeyFiter, TableOperators.Or, subRowKeyFilter);
                }
            }

            var filter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, rowKeyFiter);

            var teamDataEntities = await this.GetAllAsync(filter);

            return teamDataEntities.Select(p => p.TeamId);
        }
    }
}
