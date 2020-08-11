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
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

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
        private readonly HandleWarningActivity handleWarningActivity;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForRosterActivity"/> class.
        /// </summary>
        /// <param name="botAdapter">The bot adapter.</param>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="localizer">Localization service.</param>
        /// <param name="handleWarningActivity">handle warning activity.</param>
        public GetRecipientDataListForRosterActivity(
            BotFrameworkHttpAdapter botAdapter,
            IOptions<BotOptions> botOptions,
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository,
            IStringLocalizer<Strings> localizer,
            HandleWarningActivity handleWarningActivity)
        {
            this.botAdapter = botAdapter;
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.notificationDataRepository = notificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.localizer = localizer;
            this.handleWarningActivity = handleWarningActivity;
        }

        /// <summary>
        /// Run the activity.
        /// Gets recipient data list (team rosters).
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="teamDataEntity">Team data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
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
                var format = this.localizer.GetString("FailedToGetMembersForTeamFormat");
                var errorMessage = string.Format(format, teamDataEntity.TeamId, ex.Message);

                log.LogError(ex, errorMessage);
                await this.handleWarningActivity.RunAsync(context, notificationDataEntityId, errorMessage);
            }
        }

        /// <summary>
        /// This method represents the "get recipient data list for roster" durable activity.
        /// 1). It gets the recipient data list for a team's roster.
        /// 2). It initializes the sent notification data table with a row for each member in that roster.
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
                    input.TeamDataEntity.TeamId,
                    input.TeamDataEntity.TenantId);

                var sentNotificationDataEntities = roster
                    .Select(userDataEntity =>
                    {
                        return userDataEntity.CreateInitialSentNotificationDataEntity(
                            partitionKey: input.NotificationDataEntityId);
                    });

                await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(sentNotificationDataEntities);
            }
            catch (Exception ex)
            {
                var format = this.localizer.GetString("FailedToGetMembersForTeamFormat");
                var errorMessage = string.Format(format, input.TeamDataEntity.TeamId, ex.Message);

                log.LogError(ex, errorMessage);

                await this.notificationDataRepository
                    .SaveWarningInNotificationDataEntityAsync(input.NotificationDataEntityId, errorMessage);
            }
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
                    var members = await this.GetMembersAsync(turnContext, cancellationToken);

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

        /// <summary>
        /// Fetches the roster with the new paginated calls to handles larger teams.
        /// https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/get-teams-context?tabs=dotnet#fetching-the-roster-or-user-profile.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects.</param>
        /// <returns>The roster fetched by calling the new paginated SDK API.</returns>
        private async Task<IEnumerable<TeamsChannelAccount>> GetMembersAsync(
            ITurnContext turnContext,
            CancellationToken cancellationToken)
        {
            var members = new List<TeamsChannelAccount>();
            string continuationToken = null;
            const int pageSize = 500;

            do
            {
                var currentPage = await TeamsInfo.GetPagedMembersAsync(
                    turnContext,
                    pageSize,
                    continuationToken,
                    cancellationToken);
                continuationToken = currentPage.ContinuationToken;
                members.AddRange(currentPage.Members);
            }
            while (continuationToken != null && !cancellationToken.IsCancellationRequested);

            return members;
        }
    }
}