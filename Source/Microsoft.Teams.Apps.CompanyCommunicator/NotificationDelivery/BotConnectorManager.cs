// <copyright file="BotConnectorManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using System;
    using Microsoft.Bot.Connector;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Bot Connector Client manager.
    /// </summary>
    public class BotConnectorManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BotConnectorManager"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET core configuration service.</param>
        public BotConnectorManager(IConfiguration configuration)
        {
            var botServiceUrl = "https://smba.trafficmanager.net/amer/"; // todo: remove the hard-coded value.
            var botAppId = configuration.GetValue<string>("MicrosoftAppId");
            var botAppPassword = configuration.GetValue<string>("MicrosoftAppPassword");

            this.ConnectorClient = new ConnectorClient(
                new Uri(botServiceUrl),
                botAppId,
                botAppPassword);
        }

        /// <summary>
        /// Gets bot connector client instance.
        /// </summary>
        public ConnectorClient ConnectorClient { get; }
    }
}