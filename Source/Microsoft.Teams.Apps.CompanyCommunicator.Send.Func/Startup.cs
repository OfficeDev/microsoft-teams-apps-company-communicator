// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System.IO;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.NotificationDelivery;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<NotificationDelivery.NotificationDelivery>();

            builder.Services.AddTransient<SendingNotificationCreator>();

            builder.Services.AddTransient<MetadataProvider>();

            builder.Services.AddTransient<AdaptiveCardCreator>();

            builder.Services.AddTransient<NotificationDataRepository>();

            builder.Services.AddTransient<UserDataRepository>();

            builder.Services.AddTransient<TeamDataRepository>();

            builder.Services.AddTransient<SendingNotificationDataRepository>();

            builder.Services.AddTransient<TableRowKeyGenerator>();

            var configuration = this.BuildLocalConfiguration();
            builder.Services.AddSingleton(configuration);
        }

        private IConfiguration BuildLocalConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .Build();
            return configuration;
        }
    }
}