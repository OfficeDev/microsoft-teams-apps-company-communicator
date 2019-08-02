// <copyright file="NotificationPreview.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.User;

    /// <summary>
    /// Notification preview service.
    /// </summary>
    public class NotificationPreview
    {
        private static readonly string MsTeamsChannelId = "msteams";

        private readonly string botAppId;
        private readonly AdaptiveCardCreator adaptiveCardCreator;
        private readonly UserDataRepository userDataRepository;
        private readonly BotFrameworkHttpAdapter botFrameworkHttpAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationPreview"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration service.</param>
        /// <param name="adaptiveCardCreator">Adaptive card creator service.</param>
        /// <param name="userDataRepository">User data repository service.</param>
        /// <param name="botFrameworkHttpAdapter">Bot framework http adapter instance.</param>
        public NotificationPreview(
            IConfiguration configuration,
            AdaptiveCardCreator adaptiveCardCreator,
            UserDataRepository userDataRepository,
            BotFrameworkHttpAdapter botFrameworkHttpAdapter)
        {
            this.botAppId = configuration["MicrosoftAppId"];
            if (string.IsNullOrEmpty(this.botAppId))
            {
                throw new ApplicationException("MicrosftAppId setting is not set properly in the configuration.");
            }

            this.adaptiveCardCreator = adaptiveCardCreator;
            this.userDataRepository = userDataRepository;
            this.botFrameworkHttpAdapter = botFrameworkHttpAdapter;
        }

        /// <summary>
        /// Preview a draft notificaiton.
        /// </summary>
        /// <param name="previewerAadId">The previewer's Aad id.</param>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task Preview(string previewerAadId, NotificationEntity draftNotificationEntity)
        {
            if (string.IsNullOrWhiteSpace(previewerAadId))
            {
                throw new ArgumentException("Null previewer id.");
            }

            if (draftNotificationEntity == null)
            {
                throw new ArgumentException("Null draft notification entity.");
            }

            // Create bot conversation reference.
            var conversationReference = await this.PrepareConversationReferenceAsync(previewerAadId);

            // Ensure the bot service url is trusted.
            MicrosoftAppCredentials.TrustServiceUrl(conversationReference.ServiceUrl);

            // Trigger bot to send the adaptive card.
            await this.botFrameworkHttpAdapter.ContinueConversationAsync(
                this.botAppId,
                conversationReference,
                async (ctx, ct) =>
                {
                    var reply = this.CreateReply(draftNotificationEntity);
                    await ctx.SendActivityAsync(reply);
                },
                CancellationToken.None);
        }

        private async Task<ConversationReference> PrepareConversationReferenceAsync(string previewerAadId)
        {
            var userDataEntity = await this.userDataRepository.GetAsync(
                PartitionKeyNames.Metadata.UserData,
                previewerAadId);
            if (userDataEntity == null)
            {
                throw new ApplicationException("Previewer's user data doesn't exist in data storage.");
            }

            var channelAccount = new ChannelAccount
            {
                Id = $"28:{this.botAppId}",
            };

            var conversationAccount = new ConversationAccount
            {
                ConversationType = "personal",
                Id = userDataEntity.ConversationId,
                TenantId = userDataEntity.TenantId,
            };

            var result = new ConversationReference
            {
                Bot = channelAccount,
                ChannelId = MsTeamsChannelId,
                Conversation = conversationAccount,
                ServiceUrl = userDataEntity.ServiceUrl,
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