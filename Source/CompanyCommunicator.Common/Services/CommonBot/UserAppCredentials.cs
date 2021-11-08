// <copyright file="UserAppCredentials.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    using System;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// A user Microsoft app credentials object.
    /// </summary>
    public class UserAppCredentials : MicrosoftAppCredentials
    {
        private readonly bool useCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAppCredentials"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        public UserAppCredentials(IOptions<BotOptions> botOptions)
            : base(
                  appId: botOptions.Value.UserAppId,
                  password: botOptions.Value.UserAppPassword)
        {
            botOptions = botOptions ?? throw new ArgumentNullException(nameof(botOptions));
            this.useCertificate = botOptions.Value.UseCertificate;
        }

        /// <summary>
        /// Checks if authentication is to be done using certificate.
        /// </summary>
        /// <returns>Boolean indicating if authentication type is certifcate.</returns>
        public bool IsCertificateAuthenticationEnabled()
        {
            return this.useCertificate;
        }
    }
}
