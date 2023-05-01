// <copyright file="CCBotAdapter.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;

    /// <summary>
    /// Bot framework http adapter instance.
    /// </summary>
    public class CCBotAdapter : CCBotAdapterBase
    {
        private readonly ICertificateProvider certificateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CCBotAdapter"/> class.
        /// </summary>
        /// <param name="botFrameworkAuthentication">credential provider.</param>
        public CCBotAdapter(
            ICertificateProvider certificateProvider,
            BotFrameworkAuthentication botFrameworkAuthentication)
            : base(botFrameworkAuthentication)
        {
            this.certificateProvider = certificateProvider;
        }

        /// <inheritdoc/>
        public override async Task CreateConversationUsingCertificateAsync(string channelId, string serviceUrl, AppCredentials appCredentials, ConversationParameters conversationParameters, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            var cert = await this.certificateProvider.GetCertificateAsync(appCredentials.MicrosoftAppId);
            var options = new CertificateAppCredentialsOptions()
            {
                AppId = appCredentials.MicrosoftAppId,
                ClientCertificate = cert,
            };

            await this.CreateConversationAsync(appCredentials.MicrosoftAppId, channelId, serviceUrl, null/*audience*/, conversationParameters, callback, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task CreateConversationUsingSecretAsync(string channelId, string serviceUrl, MicrosoftAppCredentials credentials, ConversationParameters conversationParameters, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            await this.CreateConversationAsync(credentials.MicrosoftAppId, channelId, serviceUrl, null/*audience*/, conversationParameters, callback, cancellationToken);
        }
    }
}
