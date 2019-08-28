// <copyright file="MetadataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The class provides the user data by using the team data captured in the bot.
    /// </summary>
    public class MetadataProvider
    {
        private readonly UserDataRepository userDataRepository;
        private readonly TeamDataRepository teamDataRepository;
        private readonly ContinueBotConversationService continueBotConversationService;
        private readonly CompanyCommunicatorBotAdapter companyCommunicatorBotAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataProvider"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository service.</param>
        /// <param name="teamDataRepository">Team Data repository service.</param>
        /// <param name="continueBotConversationService">Continue bot conversation service.</param>
        /// <param name="companyCommunicatorBotAdapter">Company communicator bot adapter.</param>
        public MetadataProvider(
            UserDataRepository userDataRepository,
            TeamDataRepository teamDataRepository,
            ContinueBotConversationService continueBotConversationService,
            CompanyCommunicatorBotAdapter companyCommunicatorBotAdapter)
        {
            this.userDataRepository = userDataRepository;
            this.teamDataRepository = teamDataRepository;
            this.continueBotConversationService = continueBotConversationService;
            this.companyCommunicatorBotAdapter = companyCommunicatorBotAdapter;
        }

        /// <summary>
        /// Get all user data.
        /// </summary>
        /// <returns>User data dictionary.</returns>
        public async Task<Dictionary<string, UserDataEntity>> GetUserDataDictionaryAsync()
        {
            var userDataEntities = await this.userDataRepository.GetAllAsync();
            var result = new Dictionary<string, UserDataEntity>();
            foreach (var userDataEntity in userDataEntities)
            {
                result.Add(userDataEntity.AadId, userDataEntity);
            }

            return result;
        }

        /// <summary>
        /// Get all teams' roster.
        /// </summary>
        /// <returns>All teams' roster.</returns>
        public async Task<IDictionary<string, UserDataEntity>> GetAllTeamsRostersAsync()
        {
            var teamDataEntities = await this.teamDataRepository.GetAllAsync();

            var rosterUserDataEntityDictionary = await this.GetTeamsRosterAsync(teamDataEntities);

            return rosterUserDataEntityDictionary;
        }

        /// <summary>
        /// Get multiple teams' roster.
        /// </summary>
        /// <param name="teamIds">List of team ids.</param>
        /// <returns>Roster of the multiple teams.</returns>
        public async Task<IDictionary<string, UserDataEntity>> GetTeamsRostersAsync(IEnumerable<string> teamIds)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);

            var rosterUserDataEntityDictionary = await this.GetTeamsRosterAsync(teamDataEntities);

            return rosterUserDataEntityDictionary;
        }

        /// <summary>
        /// Deduplicate user data.
        /// </summary>
        /// <param name="rosterUserDataEntityDictionary">User Data Dictionary from roster.</param>
        /// <param name="usersUserDataEntityDictionary">User Data Dictionary from user data table.</param>
        public void AddRosterDictionaryToUserDictionary(
            IDictionary<string, UserDataEntity> rosterUserDataEntityDictionary,
            IDictionary<string, UserDataEntity> usersUserDataEntityDictionary)
        {
            foreach (var rosterUserKeyValuePair in rosterUserDataEntityDictionary)
            {
                if (!usersUserDataEntityDictionary.ContainsKey(rosterUserKeyValuePair.Key))
                {
                    usersUserDataEntityDictionary.Add(rosterUserKeyValuePair.Key, rosterUserKeyValuePair.Value);
                }
                else
                {
                    // Want to merge the two user data entities to backfill data that comes from the roster call
                    // (e.g. UPN, email, etc.) to the user data repo while keeping the conversation ID.
                    var conversationId = usersUserDataEntityDictionary[rosterUserKeyValuePair.Key].ConversationId;
                    var rosterUserDataEntity = rosterUserKeyValuePair.Value;
                    rosterUserDataEntity.ConversationId = conversationId;
                    usersUserDataEntityDictionary[rosterUserKeyValuePair.Key] = rosterUserDataEntity;
                }
            }
        }

        /// <summary>
        /// Creates user data entities for a list of team IDs.
        /// </summary>
        /// <param name="teamIds">Team IDs.</param>
        /// <returns>List of user data entities.</returns>
        public async Task<IEnumerable<UserDataEntity>> GetTeamsReceiverEntities(IEnumerable<string> teamIds)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);

            IList<UserDataEntity> teamReceiverEntities = new List<UserDataEntity>();

            foreach (var teamDataEntity in teamDataEntities)
            {
                teamReceiverEntities.Add(
                    new UserDataEntity
                    {
                        AadId = teamDataEntity.TeamId,
                        ConversationId = teamDataEntity.TeamId,
                        ServiceUrl = teamDataEntity.ServiceUrl,
                    });
            }

            return teamReceiverEntities;
        }

        private async Task<IDictionary<string, UserDataEntity>> GetTeamsRosterAsync(IEnumerable<TeamDataEntity> teamDataEntities)
        {
            var rosterUserDataEntityDictionary = new Dictionary<string, UserDataEntity>();

            foreach (var teamDataEntity in teamDataEntities)
            {
                var roster = await this.GetTeamRosterAsync(teamDataEntity);
                this.AddRosterToUserDataEntityDictionary(roster, rosterUserDataEntityDictionary);
            }

            return rosterUserDataEntityDictionary;
        }

        private async Task<IEnumerable<UserDataEntity>> GetTeamRosterAsync(TeamDataEntity teamDataEntity)
        {
            var members = await this.GetBotConversationMembersAsync(teamDataEntity);
            return members.Select(member =>
            {
                var userDataEntity = new UserDataEntity
                {
                    UserId = member.Id,
                    Name = member.Name,
                };

                if (member.Properties is JObject jObject)
                {
                    userDataEntity.Email = jObject["email"]?.ToString();
                    userDataEntity.Upn = jObject["userPrincipalName"]?.ToString();
                    userDataEntity.AadId = jObject["objectId"].ToString();
                    userDataEntity.TenantId = jObject["tenantId"].ToString();
                    userDataEntity.ConversationId = null;
                    userDataEntity.ServiceUrl = teamDataEntity.ServiceUrl;
                }

                return userDataEntity;
            });
        }

        private async Task<IEnumerable<ChannelAccount>> GetBotConversationMembersAsync(TeamDataEntity teamDataEntity)
        {
            try
            {
                IList<ChannelAccount> members = null;

                async Task BotCallbackHandler(ITurnContext turnContext, CancellationToken cancellationToken) =>
                    members = await this.companyCommunicatorBotAdapter.GetConversationMembersAsync(
                        turnContext,
                        CancellationToken.None);

                await this.continueBotConversationService.ContinueBotConversationAsync(
                    teamDataEntity,
                    BotCallbackHandler);

                return members ?? new List<ChannelAccount>();
            }
            catch (Exception ex)
            {
                var errorMessageStringBuilder = new StringBuilder();
                errorMessageStringBuilder.AppendLine($"Fail to get roster for the team {teamDataEntity.TeamId}");
                errorMessageStringBuilder.AppendLine(ex.ToString());
                Console.WriteLine(errorMessageStringBuilder.ToString());

                return new List<ChannelAccount>();
            }
        }

        private void AddRosterToUserDataEntityDictionary(
            IEnumerable<UserDataEntity> roster,
            IDictionary<string, UserDataEntity> rosterUserDataEntityDictionary)
        {
            foreach (var userDataEntity in roster)
            {
                if (!rosterUserDataEntityDictionary.ContainsKey(userDataEntity.AadId))
                {
                    rosterUserDataEntityDictionary.Add(userDataEntity.AadId, userDataEntity);
                }
            }
        }
    }
}