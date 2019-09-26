// <copyright file="BotConnectorClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.BotConnectorClient
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// Bot connector client factory.
    /// </summary>
    public class BotConnectorClientFactory
    {
        private static string microsoftAppIdKeyName = "MicrosoftAppId";
        private static string microsoftAppPasswordKeyName = "MicrosoftAppPassword";
        private readonly IDictionary<string, ConnectorClient> serviceUrlToConnectorClientMap =
            new Dictionary<string, ConnectorClient>();

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

                // Please see the following link for how to retrieve configuration settings in Azure functions.
                // https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library#environment-variables
                var botAppId = BotConnectorClientFactory.GetEnvironmentVariable(BotConnectorClientFactory.microsoftAppIdKeyName);
                var botAppPassword = BotConnectorClientFactory.GetEnvironmentVariable(BotConnectorClientFactory.microsoftAppPasswordKeyName);

                var connectorClient = new ConnectorClient(
                    new Uri(serviceUrl),
                    botAppId,
                    botAppPassword);

                this.serviceUrlToConnectorClientMap.Add(serviceUrl, connectorClient);
            }

            return this.serviceUrlToConnectorClientMap[serviceUrl];
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
