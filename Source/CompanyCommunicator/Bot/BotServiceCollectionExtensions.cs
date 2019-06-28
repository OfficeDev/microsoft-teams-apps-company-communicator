// <copyright file="BotServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CompanyCommunicator.Bot
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
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

            // Create the Bot Framework Adapter.
            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, CompanyCommunicatorBot>();
        }
    }
}
