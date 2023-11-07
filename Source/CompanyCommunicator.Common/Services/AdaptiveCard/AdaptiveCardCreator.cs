// <copyright file="AdaptiveCardCreator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using AdaptiveCards;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Adaptive Card Creator service.
    /// </summary>
    public class AdaptiveCardCreator
    {
        /// <summary>
        /// Creates an adaptive card.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>An adaptive card.</returns>
        public virtual AdaptiveCard CreateAdaptiveCard(NotificationDataEntity notificationDataEntity)
        {
            return this.CreateAdaptiveCard(
                notificationDataEntity.Title,
                notificationDataEntity.ImageLink,
                notificationDataEntity.Summary,
                notificationDataEntity.Author,
                notificationDataEntity.ButtonTitle,
                notificationDataEntity.ButtonLink,
                notificationDataEntity.Buttons,
                notificationDataEntity.TrackingUrl,
                notificationDataEntity.ChannelImage,
                notificationDataEntity.ChannelTitle);
        }

        /// <summary>
        /// Create an adaptive card instance.
        /// </summary>
        /// <param name="title">The adaptive card's title value.</param>
        /// <param name="imageUrl">The adaptive card's image URL.</param>
        /// <param name="summary">The adaptive card's summary value.</param>
        /// <param name="author">The adaptive card's author value.</param>
        /// <param name="buttonTitle">The adaptive card's button title value.</param>
        /// <param name="buttonUrl">The adaptive card's button url value.</param>
        /// <param name="buttons">The adaptive card's collection of buttons.</param>
        /// <param name="trackingurl">The adaptive card read tracking url.</param>
        /// <param name="cardimage">Image for the card when targeting is enabled.</param>
        /// <param name="cardtitle">Title for the card when targeting is enabled.</param>
        /// <returns>The created adaptive card instance.</returns>
        public AdaptiveCard CreateAdaptiveCard(
            string title,
            string imageUrl,
            string summary,
            string author,
            string buttonTitle,
            string buttonUrl,
            string buttons,
            string trackingurl,
            string cardimage,
            string cardtitle)
        {
            var version = new AdaptiveSchemaVersion(1, 0);
            AdaptiveCard card = new AdaptiveCard(version);

            if (!string.IsNullOrWhiteSpace(cardimage))
            {
                card.Body.Add(new AdaptiveImage()
                {
                    Url = new Uri(cardimage, UriKind.RelativeOrAbsolute),
                });
            }

            if (!string.IsNullOrWhiteSpace(cardtitle))
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = cardtitle,
                    Wrap = true,
                });
            }

            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = title,
                Size = AdaptiveTextSize.ExtraLarge,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true,
                Separator = true,
            });

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                // allows the expansion of images in the card
                var additionalProperty = new SerializableDictionary<string, object>();
                additionalProperty.Add("msteams", new
                {
                    allowExpand = true,
                });

                card.Body.Add(new AdaptiveImage()
                {
                    Url = new Uri(imageUrl, UriKind.RelativeOrAbsolute),
                    Spacing = AdaptiveSpacing.Default,
                    Size = AdaptiveImageSize.Stretch,
                    AltText = string.Empty,
                    AdditionalProperties = additionalProperty,
                });
            }

            if (!string.IsNullOrWhiteSpace(summary))
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = summary,
                    Wrap = true,
                });
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = author,
                    Size = AdaptiveTextSize.Small,
                    Weight = AdaptiveTextWeight.Lighter,
                    Wrap = true,
                });
            }

            if (!string.IsNullOrWhiteSpace(buttonTitle)
                && !string.IsNullOrWhiteSpace(buttonUrl)
                && string.IsNullOrWhiteSpace(buttons))
            {
                card.Actions.Add(new AdaptiveOpenUrlAction()
                {
                    Title = buttonTitle,
                    Url = new Uri(buttonUrl, UriKind.RelativeOrAbsolute),
                });
            }

            if (!string.IsNullOrWhiteSpace(buttons))
            {
                // enables case insensitive deserialization for card buttons
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                // add the buttons string to the buttons collection for the card
                card.Actions.AddRange(JsonSerializer.Deserialize<List<AdaptiveOpenUrlAction>>(buttons, options));
            }

            // if the tracking is disabled, trackingutl will be null/blank and the image will not be included on the card
            if (!string.IsNullOrWhiteSpace(trackingurl))
            {
                string trul = trackingurl + "/?id=[ID]&key=[KEY]";

                card.Body.Add(new AdaptiveImage()
                {
                    Url = new Uri(trul, UriKind.RelativeOrAbsolute),
                    Spacing = AdaptiveSpacing.Small,
                    Size = AdaptiveImageSize.Small,
                    IsVisible = true,
                    AltText = string.Empty,
                });
            }

            return card;
        }
    }
}
