// <copyright file="TeamDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team
{
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
            : base(configuration, "TeamsData")
        {
        }

        /// <summary>
        /// Add channel data in Table Storage.
        /// </summary>
        /// <param name="activity">Bot conversation update activity instance.</param>
        public void SaveChannelTypeData(IConversationUpdateActivity activity)
        {
            var teamDataEntity = this.ParseChannelTypeData(activity);
            if (teamDataEntity != null)
            {
                this.CreateOrUpdate(teamDataEntity);
            }
        }

        /// <summary>
        /// Remove channel data in table storage.
        /// </summary>
        /// <param name="activity">Bot conversation update activity instance.</param>
        public void RemoveChannelTypeData(IConversationUpdateActivity activity)
        {
            var teamDataEntity = this.ParseChannelTypeData(activity);
            if (teamDataEntity != null)
            {
                var found = this.Get(PartitionKeyNames.Metadata.TeamData, teamDataEntity.TeamId);
                if (found != null)
                {
                    this.Delete(found);
                }
            }
        }

        private TeamDataEntity ParseChannelTypeData(IConversationUpdateActivity activity)
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
