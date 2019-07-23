// <copyright file="UserDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The class provides the user data by using the team data captured in the bot.
    /// </summary>
    public class UserDataProvider
    {
        private readonly BotConnectorManager botConnectorManager;
        private readonly UserDataRepository userDataRepository;
        private readonly TeamDataRepository teamDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataProvider"/> class.
        /// </summary>
        /// <param name="botConnectorManager">Bot connector manager service.</param>
        /// <param name="userDataRepository">User Data repository service.</param>
        /// <param name="teamsDataRepository">Teams Data repository service.</param>
        public UserDataProvider(
            BotConnectorManager botConnectorManager,
            UserDataRepository userDataRepository,
            TeamDataRepository teamsDataRepository)
        {
            this.botConnectorManager = botConnectorManager;
            this.userDataRepository = userDataRepository;
            this.teamDataRepository = teamsDataRepository;
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
        public async Task<IEnumerable<UserDataEntity>> GetAllTeamsRosterAsync()
        {
            var result = new List<UserDataEntity>();

            var teams = await this.teamDataRepository.GetAllAsync();
            foreach (var team in teams)
            {
                var teamRoster = await this.GetTeamRosterAsync(team.TeamId);
                result.AddRange(teamRoster);
            }

            return result;
        }

        /// <summary>
        /// Get multiple teams' roster.
        /// </summary>
        /// <param name="teamIds">List of team ids.</param>
        /// <returns>Roster of the multiple teams.</returns>
        public async Task<IEnumerable<UserDataEntity>> GetTeamsRosterAsync(IEnumerable<string> teamIds)
        {
            var result = new List<UserDataEntity>();
            foreach (var teamId in teamIds)
            {
                var roster = await this.GetTeamRosterAsync(teamId);
                result.AddRange(roster);
            }

            return result;
        }

        /// <summary>
        /// Get a team's roster.
        /// </summary>
        /// <param name="teamId">Team id, e.g. "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <returns>Roster of the team with the passed in id.</returns>
        public async Task<IEnumerable<UserDataEntity>> GetTeamRosterAsync(string teamId)
        {
            try
            {
                var members = await this.botConnectorManager.ConnectorClient.Conversations.GetConversationMembersAsync(teamId);

                return members.Select(member =>
                {
                    var userDataEntity = new UserDataEntity
                    {
                        UserId = member.Id,
                        Name = member.Name,
                    };

                    if (member.Properties is JObject jObject)
                    {
                        userDataEntity.Email = jObject["email"].ToString();
                        userDataEntity.Upn = jObject["userPrincipalName"].ToString();
                        userDataEntity.AadId = jObject["objectId"].ToString();
                        userDataEntity.TenantId = jObject["tenantId"].ToString();
                        userDataEntity.ConversationId = null;
                        userDataEntity.ServiceUrl = null;
                    }

                    return userDataEntity;
                });
            }
            catch
            {
                throw new ApplicationException("The app is not authorized to access the bot service. Please send a message to the bot, then it will work.");
            }
        }

        /// <summary>
        /// Deduplicate user data.
        /// </summary>
        /// <param name="userDataDictionary">User Data Dictionary.</param>
        /// <param name="roster">User roster.</param>
        /// <returns>Deduplicated user roster.</returns>
        public IEnumerable<UserDataEntity> Deduplicate(
            Dictionary<string, UserDataEntity> userDataDictionary,
            IEnumerable<UserDataEntity> roster)
        {
            var result = new Dictionary<string, UserDataEntity>();

            foreach (var user in roster)
            {
                if (!result.ContainsKey(user.AadId))
                {
                    if (userDataDictionary.ContainsKey(user.AadId))
                    {
                        user.TenantId = userDataDictionary[user.AadId].TenantId;
                        user.ConversationId = userDataDictionary[user.AadId].ConversationId;
                    }

                    result.Add(user.AadId, user);
                }
            }

            return result.Values.ToList();
        }
    }
}
