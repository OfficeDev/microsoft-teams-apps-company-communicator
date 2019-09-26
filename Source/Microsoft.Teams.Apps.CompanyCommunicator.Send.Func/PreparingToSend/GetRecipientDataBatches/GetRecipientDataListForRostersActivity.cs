// <copyright file="GetRecipientDataListForRostersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Bot.Connector;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.BotConnectorClient;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This class contains the "get recipient data list for rosters" durable activity.
    /// </summary>
    public class GetRecipientDataListForRostersActivity
    {
        private readonly BotConnectorClientFactory botConnectorClientFactory;
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;
        private readonly TeamDataRepositoryFactory teamDataRepositoryFactory;
        private readonly SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForRostersActivity"/> class.
        /// </summary>
        /// <param name="botConnectorClientFactory">Bot connector client factory.</param>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory.</param>
        /// <param name="teamDataRepositoryFactory">Team Data repository service.</param>
        /// <param name="sentNotificationDataRepositoryFactory">Sent notification data repository factory.</param>
        public GetRecipientDataListForRostersActivity(
            BotConnectorClientFactory botConnectorClientFactory,
            NotificationDataRepositoryFactory notificationDataRepositoryFactory,
            TeamDataRepositoryFactory teamDataRepositoryFactory,
            SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory)
        {
            this.botConnectorClientFactory = botConnectorClientFactory;
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
            this.teamDataRepositoryFactory = teamDataRepositoryFactory;
            this.sentNotificationDataRepositoryFactory = sentNotificationDataRepositoryFactory;
        }

        /// <summary>
        /// Run the activity.
        /// It uses Fan-out / Fan-in pattern to get recipient data list (team rosters) in parallel.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Rosters == null || notificationDataEntity.Rosters.Count() == 0)
            {
                throw new InvalidOperationException("NotificationDataEntity's Rosters property value is null or empty!");
            }

            var teamDataEntityList = await context.CallActivityWithRetryAsync<IEnumerable<TeamDataEntity>>(
                nameof(GetRecipientDataListForRostersActivity.GetTeamDataEntitiesByIdsAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
                notificationDataEntity);

            var tasks = new List<Task>();
            foreach (var teamDataEntity in teamDataEntityList)
            {
                var task = context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                    nameof(GetRecipientDataListForRostersActivity.GetTeamRosterDataAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
                    new GetRecipientDataListForRostersActivityDTO
                    {
                        NotificationDataEntityId = notificationDataEntity.Id,
                        TeamDataEntity = teamDataEntity,
                    });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// This method represents the "get team data entity list by id" durable activity.
        /// It gets team data list by ids.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetTeamDataEntitiesByIdsAsync))]
        public async Task<IEnumerable<TeamDataEntity>> GetTeamDataEntitiesByIdsAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            if (notificationDataEntity.Rosters == null || notificationDataEntity.Rosters.Count() == 0)
            {
                throw new InvalidOperationException("NotificationDataEntity's Rosters property value is null or empty!");
            }

            var teamIds = notificationDataEntity.Rosters;

            var teamDataEntities =
                await this.teamDataRepositoryFactory.CreateRepository(true).GetTeamDataEntitiesByIdsAsync(teamIds);

            return teamDataEntities;
        }

        /// <summary>
        /// This method represents the "get team's roster" durable activity.
        /// 1). It gets recipient data list for a team's roster.
        /// 2). Initialize sent notification data in the table storage.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetTeamRosterDataAsync))]
        public async Task GetTeamRosterDataAsync(
            [ActivityTrigger] GetRecipientDataListForRostersActivityDTO input,
            ILogger log)
        {
            try
            {
                var roster = await this.GetTeamRosterRecipientDataEntityListAsync(
                    input.TeamDataEntity.ServiceUrl,
                    input.TeamDataEntity.TeamId);

                await this.sentNotificationDataRepositoryFactory.CreateRepository(true)
                    .InitializeSentNotificationDataForRecipientBatchAsync(input.NotificationDataEntityId, roster);
            }
            catch (Exception ex)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Failed to load roster for team {input.TeamDataEntity.TeamId}.");
                stringBuilder.AppendLine(ex.Message);
                var errorMessage = stringBuilder.ToString();

                log.LogError(errorMessage);

                await this.notificationDataRepositoryFactory.CreateRepository(true)
                    .SaveWarningInNotificationDataEntityAsync(input.NotificationDataEntityId, errorMessage);
            }
        }

        /// <summary>
        /// Get a team's roster.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="teamId">Team id, e.g. "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <returns>Roster of the team with the passed in id.</returns>
        private async Task<IEnumerable<UserDataEntity>> GetTeamRosterRecipientDataEntityListAsync(string serviceUrl, string teamId)
        {
            var connectorClient = this.botConnectorClientFactory.Create(serviceUrl);

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
    }
}