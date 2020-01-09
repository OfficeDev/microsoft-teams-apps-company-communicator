// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // This option is injected as IOptions<RepositoryOptions> and is used for setting
            // up the repository dependencies.
            builder.Services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    // Set the default to indicate this is an Azure Function.
                    repositoryOptions.IsAzureFunction = true;

                    // Bind any matching configuration settings to corresponding
                    // values in the options.
                    configuration.Bind(repositoryOptions);
                });

            // Needed for the NotificationDataRepository
            builder.Services.AddSingleton<TableRowKeyGenerator>();

            builder.Services.AddSingleton<NotificationDataRepository>();

            builder.Services.AddTransient<ForceCompleteNotificationDataService>();
            builder.Services.AddTransient<UpdateCountsInNotificationDataService>();
        }
    }
}
