// <copyright file="BotConnectorClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.BotConnectorClient
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Bot connector client factory.
    /// </summary>
    public class BotConnectorClientFactory
    {
        private readonly string microsoftAppId;
        private readonly string microsoftAppPassword;
        private readonly ConcurrentDictionary<string, ConnectorClient> serviceUrlToConnectorClientMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotConnectorClientFactory"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        public BotConnectorClientFactory(IOptions<BotOptions> botOptions)
        {
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.microsoftAppPassword = botOptions.Value.MicrosoftAppPassword;
            this.serviceUrlToConnectorClientMap = new ConcurrentDictionary<string, ConnectorClient>();
        }

        /// <summary>
        /// This method create a bot connector client per service URL.
        /// </summary>
        /// <param name="serviceUrl">Service URL.</param>
        /// <returns>It returns a bot connector client.</returns>
        public ConnectorClient Create(string serviceUrl)
        {
            if (!this.serviceUrlToConnectorClientMap.ContainsKey(serviceUrl))
            {
                MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

                var connectorClient = new ConnectorClient(
                    new Uri(serviceUrl),
                    this.microsoftAppId,
                    this.microsoftAppPassword);

                this.serviceUrlToConnectorClientMap.TryAdd(serviceUrl, connectorClient);
            }

            return this.serviceUrlToConnectorClientMap[serviceUrl];
        }
    }
}
