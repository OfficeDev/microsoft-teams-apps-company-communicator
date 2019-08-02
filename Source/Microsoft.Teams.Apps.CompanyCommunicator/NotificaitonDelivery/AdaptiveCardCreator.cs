// <copyright file="AdaptiveCardCreator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System;
    using AdaptiveCards;

    /// <summary>
    /// Adaptive Card Creator service.
    /// </summary>
    public class AdaptiveCardCreator
    {
        /// <summary>
        /// Create an adaptive card instance.
        /// </summary>
        /// <param name="title">The adaptive card's Title value.</param>
        /// <param name="imageUrl">The adaptive card's image URL.</param>
        /// <param name="summary">The adaptive card's summary value.</param>
        /// <param name="author">The adaptive card's author value.</param>
        /// <returns>Generated adaptive card instance.</returns>
        public AdaptiveCard CreateAdaptiveCard(string title, string imageUrl, string summary, string author)
        {
            var version = new AdaptiveSchemaVersion(1, 0);
            AdaptiveCard card = new AdaptiveCard(version);

            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = title,
                Size = AdaptiveTextSize.ExtraLarge,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true,
            });
            card.Body.Add(new AdaptiveImage()
            {
                Spacing = AdaptiveSpacing.Default,
                Url = new Uri(imageUrl),
                Size = AdaptiveImageSize.Stretch,
                Style = AdaptiveImageStyle.Person,
                AltText = string.Empty,
                PixelWidth = 400,
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = summary,
                Wrap = true,
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = author,
                Size = AdaptiveTextSize.Small,
                Weight = AdaptiveTextWeight.Lighter,
                Wrap = true,
            });

            return card;
        }
    }
}
