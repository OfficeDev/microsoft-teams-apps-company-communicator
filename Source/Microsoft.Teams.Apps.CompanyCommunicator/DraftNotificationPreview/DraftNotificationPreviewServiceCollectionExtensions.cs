// <copyright file="DraftNotificationPreviewServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.DraftNotificationPreview
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// Extension class for registering notification delivery services in DI container.
    /// </summary>
    public static class DraftNotificationPreviewServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register notification delivery services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddDraftNotificationPreview(this IServiceCollection services)
        {
            services.AddTransient<AdaptiveCardCreator>();

            services.AddTransient<DraftNotificationPreviewService>();
        }
    }
}