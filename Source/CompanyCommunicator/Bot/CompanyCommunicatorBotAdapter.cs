// <copyright file="CompanyCommunicatorBotAdapter.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;

    /// <summary>
    /// The Company Communicator Bot Adapter.
    /// </summary>
    public class CompanyCommunicatorBotAdapter : BotFrameworkHttpAdapter
    {
        private readonly ICertificateProvider certificateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorBotAdapter"/> class.
        /// </summary>
        /// <param name="credentialProvider">Credential provider service instance.</param>
        /// <param name="companyCommunicatorBotFilterMiddleware">Teams message filter middleware instance.</param>
        /// <param name="certificateProvider">Certificate provider service instance.</param>
        public CompanyCommunicatorBotAdapter(
            ICredentialProvider credentialProvider,
            CompanyCommunicatorBotFilterMiddleware companyCommunicatorBotFilterMiddleware,
            ICertificateProvider certificateProvider)
            : base(credentialProvider)
        {
            companyCommunicatorBotFilterMiddleware = companyCommunicatorBotFilterMiddleware ?? throw new ArgumentNullException(nameof(companyCommunicatorBotFilterMiddleware));
            this.certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));

            // Middleware
            this.Use(companyCommunicatorBotFilterMiddleware);
        }

        /// <inheritdoc/>
        protected override async Task<AppCredentials> BuildCredentialsAsync(string appId, string oAuthScope = null)
        {
            appId = appId ?? throw new ArgumentNullException(nameof(appId));

            if (!this.certificateProvider.IsCertificateAuthenticationEnabled())
            {
                return await base.BuildCredentialsAsync(appId, oAuthScope);
            }

            var cert = await this.certificateProvider.GetCertificateAsync(appId);
            var options = new CertificateAppCredentialsOptions()
            {
                AppId = appId,
                ClientCertificate = cert,
                OauthScope = oAuthScope,
            };

            var certificateAppCredentials = new CertificateAppCredentials(options) as AppCredentials;
            return certificateAppCredentials;
        }
    }
}
