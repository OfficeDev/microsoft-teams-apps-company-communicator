// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.BotConnectorClient;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<BotConnectorClientFactory>();

            builder.Services.AddTransient<AdaptiveCardCreator>();

            builder.Services.AddTransient<TableRowKeyGenerator>();
            builder.Services.AddTransient<NotificationDataRepositoryFactory>();
            builder.Services.AddTransient<SendingNotificationDataRepositoryFactory>();
            builder.Services.AddTransient<SentNotificationDataRepositoryFactory>();
            builder.Services.AddTransient<UserDataRepositoryFactory>();
            builder.Services.AddTransient<TeamDataRepositoryFactory>();

            builder.Services.AddTransient<SendQueue>();
            builder.Services.AddTransient<DataQueue>();
        }
    }
}