// <copyright file="NotificatinDeliveryServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension class for registering notification delivery services in DI container.
    /// </summary>
    public static class NotificatinDeliveryServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register notification delivery services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddNotificationDelivery(this IServiceCollection services)
        {
            services.AddSingleton<BotConnectorManager>();

            services.AddTransient<NotificationDelivery>();

            services.AddTransient<ActiveNotificationCreator>();

            services.AddTransient<UserDataProvider>();

            services.AddSingleton<MessageQueue>();
        }
    }
}