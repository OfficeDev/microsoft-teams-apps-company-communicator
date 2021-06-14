// <copyright file="CertificateProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This class implements ICertficateProvider, which is used to retrieve certificates.
    /// </summary>
    public class CertificateProvider : ICertificateProvider
    {
        private readonly Dictionary<string, string> thumbprints;
        private readonly bool useCertificate;
        private readonly ILogger<CertificateProvider> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateProvider"/> class.
        /// A constructor that accepts a map of bot id list and credentials.
        /// </summary>
        /// <param name="botOptions">bot options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public CertificateProvider(IOptions<BotOptions> botOptions, ILoggerFactory loggerFactory)
        {
            botOptions = botOptions ?? throw new ArgumentNullException(nameof(botOptions));
            this.logger = loggerFactory?.CreateLogger<CertificateProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.useCertificate = botOptions.Value.UseCertificate;
            if (this.useCertificate)
            {
                this.thumbprints = this.CreateThumbprintMap(botOptions.Value);
            }
        }

        /// <inheritdoc/>
        public X509Certificate2 GetCertificate(string appId)
        {
            appId = appId ?? throw new ArgumentNullException(nameof(appId));

            var thumbprint = this.thumbprints.ContainsKey(appId) ? this.thumbprints[appId] : null;

            if (string.IsNullOrEmpty(thumbprint))
            {
                this.logger.LogError("No thumbprint found.");
                return null;
            }

            thumbprint = thumbprint ?? throw new InvalidOperationException("No thumbprint found.");
            using var clientCertificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            clientCertificateStore.Open(OpenFlags.ReadOnly);
            var certificates = clientCertificateStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            clientCertificateStore.Close();
            if (certificates == null || certificates.Count < 1)
            {
                this.logger.LogError($"No SSL certificate found for app : {appId}");
                return null;
            }

            return certificates[0];
        }

        /// <inheritdoc/>
        public bool IsCertificateAuthenticationEnabled()
        {
            return this.useCertificate;
        }

        private Dictionary<string, string> CreateThumbprintMap(BotOptions botOptions)
        {
            var thumbprints = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(botOptions.UserAppId))
            {
                this.logger.LogWarning("User app id not found.");
            }
            else
            {
                thumbprints.Add(botOptions.UserAppId, botOptions.UserAppThumbprint);
            }

            if (string.IsNullOrEmpty(botOptions.AuthorAppId))
            {
                this.logger.LogWarning("Author app id not found.");
            }
            else
            {
                thumbprints.Add(botOptions.AuthorAppId, botOptions.AuthorAppThumbprint);
            }

            if (string.IsNullOrEmpty(botOptions.MicrosoftAppId))
            {
                this.logger.LogWarning("Microsoft app id not found.");
            }
            else
            {
                thumbprints.Add(botOptions.MicrosoftAppId, botOptions.MicrosoftAppThumbprint);
            }

            return thumbprints;
        }
    }
}
