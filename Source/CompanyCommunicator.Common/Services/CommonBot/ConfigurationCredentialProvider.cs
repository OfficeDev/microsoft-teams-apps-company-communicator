// <copyright file="ConfigurationCredentialProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Options;
    using Microsoft.Rest;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;

    /// <summary>
    /// This class implements ICredentialProvider, which is used by the bot framework to retrieve credential info.
    /// </summary>
    public class ConfigurationCredentialProvider : ServiceClientCredentialsFactory
    {
        private readonly Dictionary<string, ServiceClientCredentialsFactory> credentials;
        private readonly ICertificateProvider certificateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationCredentialProvider"/> class.
        /// A constructor that accepts a map of bot id list and credentials.
        /// </summary>
        /// <param name="botOptions">bot options.</param>
        /// <param name="certificateProvider">Cert provider.</param>
        public ConfigurationCredentialProvider(
            IOptions<BotOptions> botOptions,
            ICertificateProvider certificateProvider)
        {
            botOptions = botOptions ?? throw new ArgumentNullException(nameof(botOptions));
            this.credentials = new Dictionary<string, ServiceClientCredentialsFactory>();
            if (!string.IsNullOrEmpty(botOptions.Value.UserAppId))
            {
                var appId = botOptions.Value.UserAppId;
                var password = botOptions.Value.UserAppPassword;
                var credFactory = new PasswordServiceClientCredentialFactory(appId, password, string.Empty, null, null);
                this.credentials.Add(appId, credFactory);
            }

            if (!string.IsNullOrEmpty(botOptions.Value.AuthorAppId))
            {
                var appId = botOptions.Value.AuthorAppId;
                var password = botOptions.Value.AuthorAppPassword;
                var credFactory = new PasswordServiceClientCredentialFactory(appId, password, string.Empty, null, null);
                this.credentials.Add(appId, credFactory);
            }

            this.certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        }

        /// <summary>
        /// Validates an app ID.
        /// </summary>
        /// <param name="appId">The app ID to validate.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful, the result is true if <paramref name="appId"/>
        /// is valid for the controller; otherwise, false.</remarks>
        public override Task<bool> IsValidAppIdAsync(string appId, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.credentials.ContainsKey(appId));
        }

        /// <summary>
        /// Checks whether bot authentication is disabled.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful and bot authentication is disabled, the result
        /// is true; otherwise, false.
        /// </remarks>
        public override Task<bool> IsAuthenticationDisabledAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(!this.credentials.Any());
        }


        public async override Task<ServiceClientCredentials> CreateCredentialsAsync(string appId, string audience, string loginEndpoint, bool validateAuthority, CancellationToken cancellationToken)
        {
            appId = appId ?? throw new ArgumentNullException(nameof(appId));

            if (this.certificateProvider.IsCertificateAuthenticationEnabled())
            {
                var cert = await this.certificateProvider.GetCertificateAsync(appId);
                var options = new CertificateAppCredentialsOptions()
                {
                    AppId = appId,
                    ClientCertificate = cert,
                    OauthScope = null,
                };

                var certificateAppCredentials = new CertificateAppCredentials(options) as AppCredentials;
                return certificateAppCredentials;
            }
            else
            {
                this.credentials.TryGetValue(appId, out ServiceClientCredentialsFactory factory);
                return await factory.CreateCredentialsAsync(appId, audience, loginEndpoint, validateAuthority, cancellationToken);
            }
        }
    }
}
