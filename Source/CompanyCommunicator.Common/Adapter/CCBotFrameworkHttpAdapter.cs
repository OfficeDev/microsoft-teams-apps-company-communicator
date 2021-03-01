// <copyright file="CCBotFrameworkHttpAdapter.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Bot framework http adapter instance.
    /// </summary>
    public class CCBotFrameworkHttpAdapter : BotFrameworkHttpAdapter, ICCBotFrameworkHttpAdapter
    {
        private readonly ICredentialProvider credentialProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CCBotFrameworkHttpAdapter"/> class.
        /// </summary>
        /// <param name="credentialProvider">credential provider.</param>
        public CCBotFrameworkHttpAdapter(ICredentialProvider credentialProvider)
            : base(credentialProvider)
        {
            this.credentialProvider = credentialProvider;
        }

        /// <inheritdoc/>
        public override Task CreateConversationAsync(string channelId, string serviceUrl, MicrosoftAppCredentials credentials, ConversationParameters conversationParameters, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            return base.CreateConversationAsync(channelId, serviceUrl, credentials, conversationParameters, callback, cancellationToken);
        }
    }
}
