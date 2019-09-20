// <copyright file="MetadataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
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
    /// This class is the data provider for "prepare to send" durable orchestration.
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
        internal async Task<IEnumerable<UserDataEntity>> GetUserDataEntityListAsync()
        {
            var userDataEntities = await this.userDataRepository.GetAllAsync();

            return userDataEntities;
        }

        /// <summary>
        /// Get team data entities by team ids.
        /// </summary>
        /// <param name="teamIds">Team ids.</param>
        /// <returns>It returns the team data entities matching the incoming ids.</returns>
        internal async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntityListByIdsAsync(IEnumerable<string> teamIds)
        {
            var teamDataEntities = await this.teamDataRepository.GetTeamDataEntitiesByIdsAsync(teamIds);
            return teamDataEntities;
        }

        /// <summary>
        /// Get a team's roster.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="teamId">Team id, e.g. "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <returns>Roster of the team with the passed in id.</returns>
        internal async Task<IEnumerable<UserDataEntity>> GetTeamRosterRecipientDataEntityListAsync(string serviceUrl, string teamId)
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

        /// <summary>
        /// Get teams' recipient data entity list.
        /// </summary>
        /// <param name="teamIds">Team IDs.</param>
        /// <returns>List of recipient data entity (user data entities).</returns>
        internal async Task<List<UserDataEntity>> GetTeamsRecipientDataEntityListAsync(IEnumerable<string> teamIds)
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
        /// Set total recipient count in notification data entity.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataList">Recipient data list.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        internal async Task SetTotalRecipientCountInNotificationDataAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataList)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.TotalMessageCount = recipientDataList.Count();
            }
        }

        /// <summary>
        /// Initialize the status in sent notification data.
        /// Set status to be 0 (initial).
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        internal async Task InitializeStatusInSentNotificationDataAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataBatch)
        {
            var sentNotificationDataEntities = recipientDataBatch
                .Select(p =>
                    new SentNotificationDataEntity
                    {
                        PartitionKey = notificationDataEntityId,
                        RowKey = p.AadId,
                        AadId = p.AadId,
                        StatusCode = 0,
                        SentDate = DateTime.UtcNow,
                    });

            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(sentNotificationDataEntities);
        }

        /// <summary>
        /// Set "sent notification data status" to be 1 for recipients.
        /// It marks that messages are already queued for the recipients in Azure service bus.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        internal async Task SetStatusInSentNotificationDataAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataBatch)
        {
            foreach (var recipientData in recipientDataBatch)
            {
                var existing = await this.sentNotificationDataRepository.GetAsync(
                    notificationDataEntityId,
                    recipientData.AadId);
                if (existing == null || existing.StatusCode == 0)
                {
                    await this.sentNotificationDataRepository.CreateOrUpdateAsync(
                        new SentNotificationDataEntity
                        {
                            PartitionKey = notificationDataEntityId,
                            RowKey = recipientData.AadId,
                            AadId = recipientData.AadId,
                            StatusCode = 1,
                            SentDate = DateTime.UtcNow,
                        });
                }
            }
        }

        /// <summary>
        /// Get all the "sent notification data" entities of a notification.
        /// The partition key is a notification's id.
        /// </summary>
        /// <param name="sentNotificationDataEntityPartitionKey">Sent notification data entity partition key.</param>
        /// <returns>A sent notifiation data entity list.</returns>
        internal async Task<IEnumerable<SentNotificationDataEntity>> GetSentNotificationDataEntityListAsync(
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
        internal async Task SaveExceptionInNotificationDataEntityAsync(
            string notificationDataEntityId,
            string errorMessage)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.ExceptionMessage =
                    this.AppendNewLine(notificationDataEntity.ExceptionMessage, errorMessage);

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }

        /// <summary>
        /// Save warning message in a notification data entity.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="warningMessage">Warning message to be saved.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        internal async Task SaveWarningInNotificationDataEntityAsync(
            string notificationDataEntityId,
            string warningMessage)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.WarningMessage =
                    this.AppendNewLine(notificationDataEntity.WarningMessage, warningMessage);

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }

        private string AppendNewLine(string originalString, string newString)
        {
            return string.IsNullOrWhiteSpace(originalString)
                ? newString
                : $"{originalString}{Environment.NewLine}{newString}";
        }
    }
}