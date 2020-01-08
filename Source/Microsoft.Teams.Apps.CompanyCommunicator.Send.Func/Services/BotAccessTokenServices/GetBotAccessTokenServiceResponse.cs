// <copyright file="GetBotAccessTokenServiceResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.BotAccessTokenServices
{
    using System;

    /// <summary>
    /// A class for the bot access token service response.
    /// </summary>
    public class GetBotAccessTokenServiceResponse
    {
        /// <summary>
        /// Gets or sets the bot access token.
        /// </summary>
        public string BotAccessToken { get; set; }

        /// <summary>
        /// Gets or sets the bot access token expiration DateTime.
        /// </summary>
        public DateTime? BotAccessTokenExpiration { get; set; }
    }
}
