// <copyright file="BotServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension class for registering bot services in DI container.
    /// </summary>
    public static class BotServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register bot services in DI container. Use this method to register bot services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddBot(this IServiceCollection services)
        {
            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Create the Company Communicator Bot Adapter.
            services.AddSingleton<CompanyCommunicatorBotAdapter>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddSingleton<IBot, CompanyCommunicatorBot>();

            // Create the Teams Data Capture service.
            services.AddSingleton<TeamsDataCapture>();

            // Create the Bot's Teams message filter middleware service.
            services.AddSingleton<CompanyCommunicatorBotFilterMiddleware>();
        }
    }
}
