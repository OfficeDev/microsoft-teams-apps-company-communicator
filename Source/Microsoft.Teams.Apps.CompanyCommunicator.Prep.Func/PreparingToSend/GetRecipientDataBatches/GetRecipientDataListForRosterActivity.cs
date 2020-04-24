// <copyright file="GetRecipientDataListForRosterActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// This class contains the "get recipient data list for roster" durable activity.
    /// This activity prepares the SentNotification data table by filling it with an initialized row
    /// for each recipient - for "roster" every member of the given team is a recipient.
    /// 1). It gets the recipient data list for a team's roster.
    /// 2). It initializes the sent notification data table with a row for each member in that roster.
    /// </summary>
    public class GetRecipientDataListForRosterActivity
    {
        private readonly BotFrameworkHttpAdapter botAdapter;
        private readonly string microsoftAppId;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForRosterActivity"/> class.
        /// </summary>
        /// <param name="botAdapter">The bot adapter.</param>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        public GetRecipientDataListForRosterActivity(
            BotFrameworkHttpAdapter botAdapter,
            IOptions<BotOptions> botOptions,
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.botAdapter = botAdapter;
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
                    ActivitySettings.CommonActivityRetryOptions,
                    new GetRecipientDataListForRosterActivityDTO
                    {
                        NotificationDataEntityId = notificationDataEntityId,
                        TeamDataEntity = teamDataEntity,
                    });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load roster for team {teamDataEntity.TeamId}: {ex.Message}";

                log.LogError(ex, errorMessage);

                await this.notificationDataRepository
                    .SaveWarningInNotificationDataEntityAsync(notificationDataEntityId, errorMessage);
            }
        }

        /// <summary>
        /// This method represents the "get recipient data list for roster" durable activity.
        /// 1). It gets the recipient data list for a team's roster.
        /// 2). It initializes the sent notification data table with a row for each member in that roster.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetRecipientDataListForRosterAsync))]
        public async Task GetRecipientDataListForRosterAsync(
            [ActivityTrigger] GetRecipientDataListForRosterActivityDTO input)
        {
            var roster = await this.GetTeamRosterRecipientDataEntityListAsync(
                input.TeamDataEntity.ServiceUrl,
                input.TeamDataEntity.TeamId,
                input.TeamDataEntity.TenantId);

            await this.sentNotificationDataRepository
                .InitializeSentNotificationDataForUserRecipientBatchAsync(input.NotificationDataEntityId, roster);
        }

        /// <summary>
        /// Get a team's roster.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="teamId">Team id, e.g. "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <param name="tenantId">Tenant id for the team and user.</param>
        /// <returns>Roster of the team with the passed in id.</returns>
        private async Task<IEnumerable<UserDataEntity>> GetTeamRosterRecipientDataEntityListAsync(
            string serviceUrl,
            string teamId,
            string tenantId)
        {
            // Set the service URL in the trusted list to ensure the SDK includes the token in the request.
            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

            var conversationReference = new ConversationReference
            {
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = teamId,
                },
            };

            IEnumerable<UserDataEntity> userDataEntitiesResult = null;

            await this.botAdapter.ContinueConversationAsync(
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

                            // Set the conversation ID to null because it is not known at this time and
                            // may not have been created yet.
                            ConversationId = null,
                            ServiceUrl = serviceUrl,
                            Email = member.Email,
                            Upn = member.UserPrincipalName,
                            AadId = member.AadObjectId,
                            TenantId = tenantId,
                        };

                        return userDataEntity;
                    });
                },
                CancellationToken.None);

            return userDataEntitiesResult;
        }
    }
}
