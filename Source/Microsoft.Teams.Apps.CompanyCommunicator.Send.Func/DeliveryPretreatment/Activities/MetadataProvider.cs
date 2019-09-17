// <copyright file="MetadataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The class provides the user data by using the team data captured in the bot.
    /// </summary>
    public class MetadataProvider
    {
        private readonly IConfiguration configuration;
        private readonly UserDataRepository userDataRepository;
        private readonly TeamDataRepository teamDataRepository;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataProvider"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="userDataRepository">User Data repository service.</param>
        /// <param name="teamDataRepository">Team Data repository service.</param>
        /// <param name="notificationDataRepository">Notification data repository service.</param>
        /// <param name="sentNotificationDataRepository">Sent notificaion data repository service.</param>
        public MetadataProvider(
            IConfiguration configuration,
            UserDataRepository userDataRepository,
            TeamDataRepository teamDataRepository,
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.configuration = configuration;
            this.userDataRepository = userDataRepository;
            this.teamDataRepository = teamDataRepository;
            this.notificationDataRepository = notificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Get all user data entity list.
        /// </summary>
        /// <returns>User data dictionary.</returns>
        public async Task<List<UserDataEntity>> GetUserDataEntityListAsync()
        {
            var userDataEntities = await this.userDataRepository.GetAllAsync();

            return userDataEntities.ToList();
        }

        /// <summary>
        /// Get all teams' roster.
        /// </summary>
        /// <returns>All teams' roster.</returns>
        public async Task<IDictionary<string, UserDataEntity>> GetAllTeamsRostersAsync()
        {
            var rosterUserDataEntityDictionary = new Dictionary<string, UserDataEntity>();

            var teams = await this.teamDataRepository.GetAllAsync();
            foreach (var team in teams)
            {
                var roster = await this.GetTeamRosterAsync(team.ServiceUrl, team.TeamId);
                this.AddRosterToUserDataEntityDictionary(roster, rosterUserDataEntityDictionary);
            }

            return rosterUserDataEntityDictionary;
        }

        /// <summary>
        /// GEt team data entities by team ids.
        /// </summary>
        /// <param name="teamIds">Team ids.</param>
        /// <returns>It returns the team data entities matching the incoming ids.</returns>
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntityListByIdsAsync(IEnumerable<string> teamIds)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);
            return teamDataEntities;
        }

        /// <summary>
        /// Get multiple teams' roster.
        /// </summary>
        /// <param name="teamIds">List of team ids.</param>
        /// <returns>Roster of the multiple teams.</returns>
        public async Task<List<UserDataEntity>> GetTeamsRostersAsync(IEnumerable<string> teamIds)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);

            var rosterUserDataEntities = new List<UserDataEntity>();

            foreach (var teamDataEntity in teamDataEntities)
            {
                var roster = await this.GetTeamRosterAsync(teamDataEntity.ServiceUrl, teamDataEntity.TeamId);

                rosterUserDataEntities.AddRange(roster);
            }

            return rosterUserDataEntities;
        }

        /// <summary>
        /// Merge a roster list to a dictionary of users.
        /// </summary>
        /// <param name="roster">Roster list.</param>
        /// <param name="rosterUserDataEntityDictionary">Dictionary of users.</param>
        public void AddRosterToUserDataEntityDictionary(
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

        /// <summary>
        /// Get a team's roster.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="teamId">Team id, e.g. "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <returns>Roster of the team with the passed in id.</returns>
        public async Task<IEnumerable<UserDataEntity>> GetTeamRosterAsync(string serviceUrl, string teamId)
        {
            try
            {
                MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

                var botAppId = this.configuration.GetValue<string>("MicrosoftAppId");
                var botAppPassword = this.configuration.GetValue<string>("MicrosoftAppPassword");

                var connectorClient = new ConnectorClient(
                    new Uri(serviceUrl),
                    botAppId,
                    botAppPassword);

                var members = await connectorClient.Conversations.GetConversationMembersAsync(teamId);

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
                        userDataEntity.ServiceUrl = serviceUrl;
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
        public async Task<List<UserDataEntity>> GetTeamsRecipientDataEntityList(IEnumerable<string> teamIds)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);

            var teamReceiverEntities = new List<UserDataEntity>();

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

        /// <summary>
        /// Initialize sent notification data for all recipient batches.
        /// </summary>
        /// <param name="sentNotificationDataEntityId">Sent notification data entity id.</param>
        /// <param name="recipientDataBatches">Recipient data batches.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task InitializeSentNotificationDataAsync(
            string sentNotificationDataEntityId,
            List<List<UserDataEntity>> recipientDataBatches)
        {
            foreach (var batch in recipientDataBatches)
            {
                await this.SetStatusInSentNotificationDataAsync(sentNotificationDataEntityId, batch);
            }

            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                sentNotificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.TotalMessageCount = recipientDataBatches.SelectMany(p => p).Count();
            }
        }

        /// <summary>
        /// Set sent notification data for a recipient batch.
        /// Set the status in sent notification data.
        /// Status 0 means initial.
        /// Status 1 means the send message is already enqued in Azure service bus.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch.</param>
        /// <param name="status">Status code.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SetStatusInSentNotificationDataAsync(
            string notificationDataEntityId,
            List<UserDataEntity> recipientDataBatch,
            int status = 0)
        {
            var sentNotificationDataEntities = recipientDataBatch.Select(p =>
            {
                return new SentNotificationDataEntity
                {
                    PartitionKey = notificationDataEntityId,
                    RowKey = p.AadId,
                    AadId = p.AadId,
                    StatusCode = status,
                };
            });

            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(sentNotificationDataEntities);
        }

        /// <summary>
        /// Get sent notification data entity list by partition key.
        /// The partition key is a notification's id.
        /// </summary>
        /// <param name="sentNotificationDataEntityPartitionKey">Sent notification data entity partition key.</param>
        /// <returns>A sent notifiation data entity list.</returns>
        public async Task<IEnumerable<SentNotificationDataEntity>> GetSentNotificationDataEntityListAsync(
            string sentNotificationDataEntityPartitionKey)
        {
            return await this.sentNotificationDataRepository.GetAllAsync(
                sentNotificationDataEntityPartitionKey);
        }

        /// <summary>
        /// Save exception error message in a notification data entity.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SaveExceptionInNotificationDataEntityAsync(
            string notificationDataEntityId,
            string errorMessage)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.ExceptionErrorMessage = errorMessage;
                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }
    }
}
