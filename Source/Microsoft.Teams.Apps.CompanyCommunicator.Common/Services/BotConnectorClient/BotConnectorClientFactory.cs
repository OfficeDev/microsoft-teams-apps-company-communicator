// <copyright file="BotConnectorClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.BotConnectorClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Bot connector client factory.
    /// </summary>
    public class BotConnectorClientFactory
    {
        private const string MicrosoftAppIdKeyName = "MicrosoftAppId";
        private const string MicrosoftAppPasswordKeyName = "MicrosoftAppPassword";
        private readonly IConfiguration configuration;
        private readonly ConcurrentDictionary<string, ConnectorClient> serviceUrlToConnectorClientMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotConnectorClientFactory"/> class.
        /// </summary>
        /// <param name="configuration">Configuration service.</param>
        public BotConnectorClientFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
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

                var botAppId = this.configuration[BotConnectorClientFactory.MicrosoftAppIdKeyName];
                var botAppPassword = this.configuration[BotConnectorClientFactory.MicrosoftAppPasswordKeyName];

                var connectorClient = new ConnectorClient(
                    new Uri(serviceUrl),
                    botAppId,
                    botAppPassword);

                this.serviceUrlToConnectorClientMap.TryAdd(serviceUrl, connectorClient);
            }

            return this.serviceUrlToConnectorClientMap[serviceUrl];
        }
    }
}
