// <copyright file="RepositoriesServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ActiveNotification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.User;

    /// <summary>
    /// Extension class for registering respository services in DI container.
    /// </summary>
    public static class RepositoriesServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register repository services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddSingleton<ActiveNotificationRepository>();
            services.AddSingleton<SentNotificationDataRepository>();
            services.AddSingleton<NotificationRepository>();
            services.AddSingleton<UserDataRepository>();
            services.AddSingleton<TeamDataRepository>();
            services.AddSingleton<TableRowKeyGenerator>();
        }
    }
}
