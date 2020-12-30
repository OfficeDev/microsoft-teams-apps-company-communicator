// <copyright file="AuthorAppCredentials.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// An author Microsoft app credentials object.
    /// </summary>
    public class AuthorAppCredentials : MicrosoftAppCredentials
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorAppCredentials"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        public AuthorAppCredentials(IOptions<BotOptions> botOptions)
            : base(
                  appId: botOptions.Value.AuthorAppId,
                  password: botOptions.Value.AuthorAppPassword)
        {
        }
    }
}
