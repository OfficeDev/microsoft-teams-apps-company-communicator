// <copyright file="AdaptiveImageWithLongUrl.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard
{
    using AdaptiveCards;
    using Newtonsoft.Json;

    /// <summary>
    /// Workaround for long data uri https://github.com/microsoft/AdaptiveCards/issues/2716.
    /// </summary>
    public class AdaptiveImageWithLongUrl : AdaptiveImage
    {
        /// <summary>
        /// Gets or sets Data URI for Image.
        /// </summary>
        [JsonProperty(PropertyName = "url", Required = Required.Always)]
        public string LongUrl { get; set; }
    }
}