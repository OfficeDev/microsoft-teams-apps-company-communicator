// <copyright file="GetRecipientDataListForRosterActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This class contains the "get recipient data list for roster" durable activity.
    /// </summary>
    public class GetRecipientDataListForRosterActivity
    {
        private readonly CommonBotAdapter commonBotAdapter;
        private readonly string microsoftAppId;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForRosterActivity"/> class.
        /// </summary>
        /// <param name="commonBotAdapter">The common bot adapter.</param>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        public GetRecipientDataListForRosterActivity(
            CommonBotAdapter commonBotAdapter,
            IOptions<BotOptions> botOptions,
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.commonBotAdapter = commonBotAdapter;
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.notificationDataRepository = notificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// It uses Fan-out / Fan-in pattern to get recipient data list (team rosters) in parallel.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="teamDataEntity">Team data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            TeamDataEntity teamDataEntity,
            ILogger log)
        {
            try
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
            catch (Exception ex)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Failed to load roster for team {teamDataEntity.TeamId}.");
                stringBuilder.AppendLine(ex.Message);
                var errorMessage = stringBuilder.ToString();

                log.LogError(errorMessage);

                await this.notificationDataRepository
                    .SaveWarningInNotificationDataEntityAsync(notificationDataEntityId, errorMessage);
            }
        }

        /// <summary>
        /// This method represents the "get recipient data list for roster" durable activity.
        /// 1). It gets recipient data list for a team's roster.
        /// 2). Initialize sent notification data in the table storage.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetRecipientDataListForRosterAsync))]
        public async Task GetRecipientDataListForRosterAsync(
            [ActivityTrigger] GetRecipientDataListForRosterActivityDTO input)
        {
            var roster = await this.GetTeamRosterRecipientDataEntityListAsync(
                input.TeamDataEntity.ServiceUrl,
                input.TeamDataEntity.TeamId);

            await this.sentNotificationDataRepository
                .InitializeSentNotificationDataForRecipientBatchAsync(input.NotificationDataEntityId, roster);
        }

        /// <summary>
        /// Get a team's roster.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="teamId">Team id, e.g. "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <returns>Roster of the team with the passed in id.</returns>
        private async Task<IEnumerable<UserDataEntity>> GetTeamRosterRecipientDataEntityListAsync(string serviceUrl, string teamId)
        {
            var conversationReference = new ConversationReference
            {
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = teamId,
                },
            };

            IEnumerable<UserDataEntity> userDataEntitiesResult = null;

            await this.commonBotAdapter.ContinueConversationAsync(
                this.microsoftAppId,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var members = await TeamsInfo.GetMembersAsync(turnContext, cancellationToken);

                    userDataEntitiesResult = members.Select(member =>
                    {
                        var userDataEntity = new UserDataEntity
                        {
                            UserId = member.Id,
                            Name = member.Name,
                        };

                        // Set the conversation ID to null because it is not known at this time and
                        // may not have been created yet.
                        userDataEntity.ConversationId = null;
                        userDataEntity.ServiceUrl = serviceUrl;
                        userDataEntity.Email = member.Email;
                        userDataEntity.Upn = member.UserPrincipalName;
                        userDataEntity.AadId = member.AadObjectId;

                        if (member.Properties is JObject jObject)
                        {
                            userDataEntity.TenantId = jObject["tenantId"].ToString();
                        }

                        return userDataEntity;
                    });
                },
                CancellationToken.None);

            return userDataEntitiesResult;
        }
    }
}
