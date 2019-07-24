// <copyright file="TeamDataRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Extensions for the respository of the team data stored in the table storage.
    /// </summary>
    public static class TeamDataRepositoryExtensions
    {
        /// <summary>
        /// Add channel data in Table Storage.
        /// </summary>
        /// <param name="teamDataRepository">The team data repository.</param>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public static async Task SaveTeamDataAsync(
            this TeamDataRepository teamDataRepository,
            IConversationUpdateActivity activity)
        {
            var teamDataEntity = TeamDataRepositoryExtensions.ParseTeamData(activity);
            if (teamDataEntity != null)
            {
                await teamDataRepository.CreateOrUpdateAsync(teamDataEntity);
            }
        }

        /// <summary>
        /// Remove channel data in table storage.
        /// </summary>
        /// <param name="teamDataRepository">The team data repository.</param>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public static async Task RemoveTeamDataAsync(
            this TeamDataRepository teamDataRepository,
            IConversationUpdateActivity activity)
        {
            var teamDataEntity = TeamDataRepositoryExtensions.ParseTeamData(activity);
            if (teamDataEntity != null)
            {
                var found = await teamDataRepository.GetAsync(PartitionKeyNames.Metadata.TeamData, teamDataEntity.TeamId);
                if (found != null)
                {
                    await teamDataRepository.DeleteAsync(found);
                }
            }
        }

        /// <summary>
        /// Get team names by Ids.
        /// </summary>
        /// <param name="teamDataRepository">The team data repository.</param>
        /// <param name="ids">Team ids.</param>
        /// <returns>Names of the teams matching incoming ids.</returns>
        public static async Task<IEnumerable<string>> GetTeamNamesByIdsAsync(
            this TeamDataRepository teamDataRepository,
            IEnumerable<string> ids)
        {
            var result = new List<string>();

            if (ids == null || ids.Count() == 0)
            {
                return result;
            }

            var batchOperations = new TableBatchOperation();
            foreach (var id in ids)
            {
                batchOperations.Add(TableOperation.Retrieve(PartitionKeyNames.Metadata.TeamData, id));
            }

            var batchResult = await teamDataRepository.Table.ExecuteBatchAsync(batchOperations);

            foreach (var singleResult in batchResult)
            {
                var entity = singleResult.Result as DynamicTableEntity;
                var name = entity?.Properties["Name"]?.ToString();
                result.Add(name);
            }

            return result;
        }

        private static TeamDataEntity ParseTeamData(IConversationUpdateActivity activity)
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
