// <copyright file="DraftNotificationPreviewService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// Draft notification preview service.
    /// </summary>
    public class DraftNotificationPreviewService
    {
        private static readonly string ThrottledErrorResponse = "Throttled";

        private readonly ContinueBotConversationService continueBotConversationService;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationPreviewService"/> class.
        /// </summary>
        /// <param name="continueBotConversationService">Continue bot conversation service.</param>
        /// <param name="adaptiveCardCreator">Adaptive card creator service.</param>
        public DraftNotificationPreviewService(
            ContinueBotConversationService continueBotConversationService,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.continueBotConversationService = continueBotConversationService;
            this.adaptiveCardCreator = adaptiveCardCreator;
        }

        /// <summary>
        /// Send a preview of a draft notification.
        /// </summary>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="teamDataEntity">The team data entity.</param>
        /// <param name="teamsChannelId">The Teams channel id.</param>
        /// <returns>It returns HttpStatusCode.OK, if this method triggers the bot service to send the adaptive card successfully.
        /// It returns HttpStatusCode.TooManyRequests, if the bot service throttled the request to send the adaptive card.</returns>
        public async Task<HttpStatusCode> SendPreviewAsync(
            NotificationDataEntity draftNotificationEntity,
            TeamDataEntity teamDataEntity,
            string teamsChannelId)
        {
            if (draftNotificationEntity == null)
            {
                throw new ArgumentNullException(nameof(draftNotificationEntity));
            }

            try
            {
                await this.continueBotConversationService.ContinueBotConversationAsync(
                    teamDataEntity,
                    teamsChannelId,
                    async (turnContext, cancellationToken) =>
                    {
                        var reply = this.CreateReply(draftNotificationEntity);
                        await turnContext.SendActivityAsync(reply);
                    });

                return HttpStatusCode.OK;
            }
            catch (ErrorResponseException e)
            {
                var errorResponse = (ErrorResponse)e.Body;
                if (errorResponse != null
                    && errorResponse.Error.Code.Equals(DraftNotificationPreviewService.ThrottledErrorResponse, StringComparison.OrdinalIgnoreCase))
                {
                    return HttpStatusCode.TooManyRequests;
                }

                throw;
            }
        }

        private IMessageActivity CreateReply(NotificationDataEntity draftNotificationEntity)
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