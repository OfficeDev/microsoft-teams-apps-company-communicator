// <copyright file="DraftNotificationPreview.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team;

    /// <summary>
    /// Draft notification preview service.
    /// </summary>
    public class DraftNotificationPreview
    {
        private static readonly string MsTeamsChannelId = "msteams";
        private static readonly string ChannelConversationType = "channel";
        private static readonly string ThrottledErrorResponse = "Throttled";

        private readonly string botAppId;
        private readonly AdaptiveCardCreator adaptiveCardCreator;
        private readonly BotFrameworkHttpAdapter botFrameworkHttpAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationPreview"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration service.</param>
        /// <param name="adaptiveCardCreator">Adaptive card creator service.</param>
        /// <param name="botFrameworkHttpAdapter">Bot framework http adapter instance.</param>
        public DraftNotificationPreview(
            IConfiguration configuration,
            AdaptiveCardCreator adaptiveCardCreator,
            BotFrameworkHttpAdapter botFrameworkHttpAdapter)
        {
            this.botAppId = configuration["MicrosoftAppId"];
            if (string.IsNullOrEmpty(this.botAppId))
            {
                throw new ApplicationException("MicrosftAppId setting is missing in the configuration.");
            }

            this.adaptiveCardCreator = adaptiveCardCreator;
            this.botFrameworkHttpAdapter = botFrameworkHttpAdapter;
        }

        /// <summary>
        /// Preview a draft notification.
        /// </summary>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="teamDataEntity">The team data entity.</param>
        /// <param name="teamsChannelId">The Teams channel id.</param>
        /// <returns>It returns HttpStatusCode.OK, if this method triggers the bot service to send adaptive card successfully.
        /// It returns HttpStatusCode.TooManyRequests, if the bot service throttled the request to send adaptive card.</returns>
        public async Task<HttpStatusCode> Preview(NotificationEntity draftNotificationEntity, TeamDataEntity teamDataEntity, string teamsChannelId)
        {
            if (draftNotificationEntity == null)
            {
                throw new ArgumentException("Null draft notification entity.");
            }

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
                await this.botFrameworkHttpAdapter.ContinueConversationAsync(
                    this.botAppId,
                    conversationReference,
                    async (ctx, ct) => await this.SendAdaptiveCardAsync(ctx, draftNotificationEntity),
                    CancellationToken.None);
                return HttpStatusCode.OK;
            }
            catch (ErrorResponseException e)
            {
                var errorResponse = (ErrorResponse)e.Body;
                if (errorResponse != null
                    && errorResponse.Error.Code.Equals(ThrottledErrorResponse, StringComparison.OrdinalIgnoreCase))
                {
                    return HttpStatusCode.TooManyRequests;
                }

                throw;
            }
        }

        private async Task SendAdaptiveCardAsync(
            ITurnContext turnContext,
            NotificationEntity draftNotificationEntity)
        {
            var reply = this.CreateReply(draftNotificationEntity);
            await turnContext.SendActivityAsync(reply);
        }

        private ConversationReference PrepareConversationReferenceAsync(TeamDataEntity teamDataEntity, string channelId)
        {
            var channelAccount = new ChannelAccount
            {
                Id = $"28:{this.botAppId}",
            };

            var conversationAccount = new ConversationAccount
            {
                ConversationType = ChannelConversationType,
                Id = channelId,
                TenantId = teamDataEntity.TenantId,
            };

            var result = new ConversationReference
            {
                Bot = channelAccount,
                ChannelId = MsTeamsChannelId,
                Conversation = conversationAccount,
                ServiceUrl = teamDataEntity.ServiceUrl,
            };

            return result;
        }

        private IMessageActivity CreateReply(NotificationEntity draftNotificationEntity)
        {
            var adaptiveCard = this.adaptiveCardCreator.CreateAdaptiveCard(
                draftNotificationEntity.Title,
                draftNotificationEntity.ImageLink,
                draftNotificationEntity.Summary,
                draftNotificationEntity.Author,
                draftNotificationEntity.ButtonTitle,
                draftNotificationEntity.ButtonLink);

            var attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = adaptiveCard,
            };

            var reply = MessageFactory.Attachment(attachment);

            return reply;
        }
    }
}