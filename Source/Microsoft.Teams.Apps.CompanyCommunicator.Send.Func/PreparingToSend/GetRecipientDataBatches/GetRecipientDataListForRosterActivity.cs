// <copyright file="GetRecipientDataListForRosterActivity.cs" company="Microsoft">
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
    /// This class contains the "get recipient data list for roster" durable activity.
    /// </summary>
    public class GetRecipientDataListForRosterActivity
    {
        private readonly BotConnectorClientFactory botConnectorClientFactory;
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;
        private readonly SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForRosterActivity"/> class.
        /// </summary>
        /// <param name="botConnectorClientFactory">Bot connector client factory.</param>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory.</param>
        /// <param name="sentNotificationDataRepositoryFactory">Sent notification data repository factory.</param>
        public GetRecipientDataListForRosterActivity(
            BotConnectorClientFactory botConnectorClientFactory,
            NotificationDataRepositoryFactory notificationDataRepositoryFactory,
            SentNotificationDataRepositoryFactory sentNotificationDataRepositoryFactory)
        {
            this.botConnectorClientFactory = botConnectorClientFactory;
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
            this.sentNotificationDataRepositoryFactory = sentNotificationDataRepositoryFactory;
        }

        /// <summary>
        /// Run the activity.
        /// It uses Fan-out / Fan-in pattern to get recipient data list (team rosters) in parallel.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="teamDataEntity">Team data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            TeamDataEntity teamDataEntity)
        {
            await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                nameof(GetRecipientDataListForRosterActivity.GetRecipientDataListForRosterAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
                new GetRecipientDataListForRosterActivityDTO
                {
                    NotificationDataEntityId = notificationDataEntityId,
                    TeamDataEntity = teamDataEntity,
                });
        }

        /// <summary>
        /// This method represents the "get recipient data list for roster" durable activity.
        /// 1). It gets recipient data list for a team's roster.
        /// 2). Initialize sent notification data in the table storage.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetRecipientDataListForRosterAsync))]
        public async Task GetRecipientDataListForRosterAsync(
            [ActivityTrigger] GetRecipientDataListForRosterActivityDTO input,
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