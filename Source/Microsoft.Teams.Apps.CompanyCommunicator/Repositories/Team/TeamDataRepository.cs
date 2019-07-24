// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

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
            : base(configuration, "TeamData")
        {
        }

        /// <summary>
        /// Add channel data in Table Storage.
        /// </summary>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SaveTeamDataAsync(IConversationUpdateActivity activity)
        {
            var teamDataEntity = this.ParseTeamData(activity);
            if (teamDataEntity != null)
            {
                await this.CreateOrUpdateAsync(teamDataEntity);
            }
        }

        /// <summary>
        /// Remove channel data in table storage.
        /// </summary>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task RemoveTeamDataAsync(IConversationUpdateActivity activity)
        {
            var teamDataEntity = this.ParseTeamData(activity);
            if (teamDataEntity != null)
            {
                var found = await this.GetAsync(PartitionKeyNames.Metadata.TeamData, teamDataEntity.TeamId);
                if (found != null)
                {
                    await this.DeleteAsync(found);
                }
            }
        }

        /// <summary>
        /// Get team names by Ids.
        /// </summary>
        /// <param name="ids">Team ids.</param>
        /// <returns>Names of the teams matching incoming ids.</returns>
        public async Task<IEnumerable<string>> GetTeamNamesByIdsAsync(IEnumerable<string> ids)
        {
            var result = new List<string>();

            if (ids == null)
            {
                return result;
            }

            var batchOperations = new TableBatchOperation();
            foreach (var id in ids)
            {
                batchOperations.Add(TableOperation.Retrieve(PartitionKeyNames.Metadata.TeamData, id));
            }

            var batchResult = await this.Table.ExecuteBatchAsync(batchOperations);

            foreach (var singleResult in batchResult)
            {
                var entity = singleResult.Result as DynamicTableEntity;
                var name = entity?.Properties["Name"]?.ToString();
                result.Add(name);
            }

            return result;
        }

        private TeamDataEntity ParseTeamData(IConversationUpdateActivity activity)
        {
            if (activity?.ChannelData is JObject jObject &&
                jObject["team"]["id"] != null &&
                !string.IsNullOrEmpty(jObject["team"]["id"].ToString()))
            {
                var teamsDataEntity = new TeamDataEntity
                {
                    PartitionKey = PartitionKeyNames.Metadata.TeamData,
                    RowKey = jObject["team"]["id"].ToString(),
                    TeamId = jObject["team"]["id"].ToString(),
                    Name = jObject["team"]["name"].ToString(),
                    ServiceUrl = activity.ServiceUrl,
                    TenantId = jObject["tenant"]["id"].ToString(),
                };

                return teamsDataEntity;
            }

            return null;
        }
    }
}
