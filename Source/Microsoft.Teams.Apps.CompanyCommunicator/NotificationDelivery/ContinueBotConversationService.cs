// <copyright file="ContinueBotConversationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// Draft notification preview service.
    /// </summary>
    public class ContinueBotConversationService
    {
        private static readonly string MsTeamsChannelId = "msteams";
        private static readonly string ChannelConversationType = "channel";
        private static readonly string ThrottledErrorResponse = "Throttled";

        private readonly string botAppId;
        private readonly CompanyCommunicatorBotAdapter companyCommunicatorBotAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinueBotConversationService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration service.</param>
        /// <param name="companyCommunicatorBotAdapter">Bot framework http adapter instance.</param>
        public ContinueBotConversationService(
            IConfiguration configuration,
            CompanyCommunicatorBotAdapter companyCommunicatorBotAdapter)
        {
            this.botAppId = configuration["MicrosoftAppId"];
            if (string.IsNullOrEmpty(this.botAppId))
            {
                throw new ApplicationException("MicrosoftAppId setting is missing in the configuration.");
            }

            this.companyCommunicatorBotAdapter = companyCommunicatorBotAdapter;
        }

        /// <summary>
        /// Continue a bot conversation.
        /// </summary>
        /// <param name="teamDataEntity">The team data entity.</param>
        /// <param name="botCallbackHandler">Bot callback handler.</param>
        /// <returns>It returns HttpStatusCode.OK, if this method triggers the bot service to send the adaptive card successfully.
        /// It returns HttpStatusCode.TooManyRequests, if the bot service throttled the request to send the adaptive card.</returns>
        public async Task<HttpStatusCode> ContinueBotConversationAsync(
            TeamDataEntity teamDataEntity,
            BotCallbackHandler botCallbackHandler)
        {
            if (teamDataEntity == null)
            {
                throw new ArgumentException("Null team data entity.");
            }

            return await this.ContinueBotConversationAsync(teamDataEntity, teamDataEntity.TeamId, botCallbackHandler);
        }

        /// <summary>
        /// Continue a bot conversation.
        /// </summary>
        /// <param name="teamDataEntity">The team data entity.</param>
        /// <param name="teamsChannelId">The Teams channel id.</param>
        /// <param name="botCallbackHandler">Bot callback handler.</param>
        /// <returns>It returns HttpStatusCode.OK, if this method triggers the bot service to send the adaptive card successfully.
        /// It returns HttpStatusCode.TooManyRequests, if the bot service throttled the request to send the adaptive card.</returns>
        public async Task<HttpStatusCode> ContinueBotConversationAsync(
            TeamDataEntity teamDataEntity,
            string teamsChannelId,
            BotCallbackHandler botCallbackHandler)
        {
            if (teamDataEntity == null)
            {
                throw new ArgumentException("Null team data entity.");
            }

            if (string.IsNullOrWhiteSpace(teamsChannelId))
            {
                throw new ArgumentException("Null channel id.");
            }

            // Create bot conversation reference.
            var conversationReference = this.PrepareConversationReferenceAsync(teamDataEntity, teamsChannelId);

            // Ensure the bot service URL is trusted.
            if (!MicrosoftAppCredentials.IsTrustedServiceUrl(conversationReference.ServiceUrl))
            {
                MicrosoftAppCredentials.TrustServiceUrl(conversationReference.ServiceUrl);
            }

            // Trigger bot to send the adaptive card.
            try
            {
                await this.companyCommunicatorBotAdapter.ContinueConversationAsync(
                    this.botAppId,
                    conversationReference,
                    botCallbackHandler,
                    CancellationToken.None);
                return HttpStatusCode.OK;
            }
            catch (ErrorResponseException e)
            {
                var errorResponse = (ErrorResponse)e.Body;
                if (errorResponse != null
                    && errorResponse.Error.Code.Equals(ContinueBotConversationService.ThrottledErrorResponse, StringComparison.OrdinalIgnoreCase))
                {
                    return HttpStatusCode.TooManyRequests;
                }

                throw;
            }
        }

        private ConversationReference PrepareConversationReferenceAsync(
            TeamDataEntity teamDataEntity,
            string teamsChannelId)
        {
            var channelAccount = new ChannelAccount
            {
                Id = $"28:{this.botAppId}",
            };

            var conversationAccount = new ConversationAccount
            {
                ConversationType = ContinueBotConversationService.ChannelConversationType,
                Id = teamsChannelId,
                TenantId = teamDataEntity.TenantId,
            };

            var conversationReference = new ConversationReference
            {
                Bot = channelAccount,
                ChannelId = ContinueBotConversationService.MsTeamsChannelId,
                Conversation = conversationAccount,
                ServiceUrl = teamDataEntity.ServiceUrl,
            };

            return conversationReference;
        }
    }
}