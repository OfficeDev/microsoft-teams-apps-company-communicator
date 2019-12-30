// <copyright file="RepositoriesServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Extension class for registering repository services in DI container.
    /// </summary>
    public static class RepositoriesServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register repository services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddRepositories(this IServiceCollection services)
        {
            services.Configure<RepositoryOptions>(repositoryOptions =>
            {
                repositoryOptions.IsAzureFunction = false;
            });

            services.AddSingleton<SendingNotificationDataRepository>();
            services.AddSingleton<SentNotificationDataRepository>();
            services.AddSingleton<NotificationDataRepository>();
            services.AddSingleton<UserDataRepository>();
            services.AddSingleton<TeamDataRepository>();

            services.AddTransient<TableRowKeyGenerator>();
        }
    }
}
