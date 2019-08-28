// <copyright file="NotificationDeliveryServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension class for registering notification delivery services in DI container.
    /// </summary>
    public static class NotificationDeliveryServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register notification delivery services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddNotificationDelivery(this IServiceCollection services)
        {
            services.AddTransient<NotificationDelivery>();

            services.AddTransient<SendingNotificationCreator>();

            services.AddTransient<MetadataProvider>();

            services.AddTransient<AdaptiveCardCreator>();

            services.AddTransient<DraftNotificationPreviewService>();
        }
    }
}