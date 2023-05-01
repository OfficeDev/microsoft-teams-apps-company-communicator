// <copyright file="CCBotAdapterBase.cs" company="Microsoft">
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
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Bot Framework Http Adapter interface.
    /// </summary>
    public abstract class CCBotAdapterBase : CloudAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CCBotAdapterBase"/> class.
        /// </summary>
        /// <param name="botFrameworkAuthentication">Bot Framework Authentication.</param>
        /// <param name="logger">Logger</param>
        protected CCBotAdapterBase(BotFrameworkAuthentication botFrameworkAuthentication, ILogger logger = null)
            : base(botFrameworkAuthentication, logger)
        {
        }

        /// <summary>
        /// Creates a conversation using app secret on the specified channel.
        /// </summary>
        /// <param name="channelId">The ID for the channel.</param>
        /// <param name="serviceUrl">The channel's service URL endpoint.</param>
        /// <param name="credentials">The application credentials for the bot.</param>
        /// <param name="conversationParameters">The conversation information to use to create the conversation.</param>
        /// <param name="callback">The method to call for the resulting bot turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public abstract Task CreateConversationUsingSecretAsync(string channelId, string serviceUrl, MicrosoftAppCredentials credentials, ConversationParameters conversationParameters, BotCallbackHandler callback, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a conversation using app certificate on the specified channel.
        /// This method can be used to use certificates for authentication.
        /// </summary>
        /// <param name="channelId">The ID for the channel.</param>
        /// <param name="serviceUrl">The channel's service URL endpoint.</param>
        /// <param name="appCredentials">The application credentials for the bot.</param>
        /// <param name="conversationParameters">The conversation information to use to create the conversation.</param>
        /// <param name="callback">The method to call for the resulting bot turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public abstract Task CreateConversationUsingCertificateAsync(string channelId, string serviceUrl, AppCredentials appCredentials, ConversationParameters conversationParameters, BotCallbackHandler callback, CancellationToken cancellationToken);
    }
}
