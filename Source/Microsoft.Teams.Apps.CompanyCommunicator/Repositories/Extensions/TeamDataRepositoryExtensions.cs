// <copyright file="TeamDataRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions
{
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// Extensions for the repository of the team data stored in the table storage.
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
            this ITeamDataRepository teamDataRepository,
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
            this ITeamDataRepository teamDataRepository,
            IConversationUpdateActivity activity)
        {
            var teamDataEntity = TeamDataRepositoryExtensions.ParseTeamData(activity);
            if (teamDataEntity != null)
            {
                var found = await teamDataRepository.GetAsync(TeamDataTableNames.TeamDataPartition, teamDataEntity.TeamId);
                if (found != null)
                {
                    await teamDataRepository.DeleteAsync(found);
                }
            }
        }

        private static TeamDataEntity ParseTeamData(IConversationUpdateActivity activity)
        {
            var channelData = activity.GetChannelData<TeamsChannelData>();
            if (channelData != null)
            {
                var teamsDataEntity = new TeamDataEntity
                {
                    PartitionKey = TeamDataTableNames.TeamDataPartition,
                    RowKey = channelData.Team.Id,
                    TeamId = channelData.Team.Id,
                    Name = channelData.Team.Name,
                    ServiceUrl = activity.ServiceUrl,
                    TenantId = channelData.Tenant.Id,
                };

                return teamsDataEntity;
            }

            return null;
        }
    }
}
