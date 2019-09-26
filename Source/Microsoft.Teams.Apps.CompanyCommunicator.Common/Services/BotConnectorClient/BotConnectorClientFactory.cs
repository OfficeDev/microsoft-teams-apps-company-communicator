// <copyright file="BotConnectorClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.BotConnectorClient
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Bot connector client factory.
    /// </summary>
    public class BotConnectorClientFactory
    {
        private static string microsoftAppIdKeyName = "MicrosoftAppId";
        private static string microsoftAppPasswordKeyName = "MicrosoftAppPassword";
        private readonly IConfiguration configuration;
        private readonly IDictionary<string, ConnectorClient> serviceUrlToConnectorClientMap =
            new Dictionary<string, ConnectorClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BotConnectorClientFactory"/> class.
        /// </summary>
        /// <param name="configuration">Configuration service.</param>
        public BotConnectorClientFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
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

                var botAppId = this.configuration[BotConnectorClientFactory.microsoftAppIdKeyName];
                var botAppPassword = this.configuration[BotConnectorClientFactory.microsoftAppPasswordKeyName];

                var connectorClient = new ConnectorClient(
                    new Uri(serviceUrl),
                    botAppId,
                    botAppPassword);

                this.serviceUrlToConnectorClientMap.Add(serviceUrl, connectorClient);
            }

            return this.serviceUrlToConnectorClientMap[serviceUrl];
        }
    }
}
