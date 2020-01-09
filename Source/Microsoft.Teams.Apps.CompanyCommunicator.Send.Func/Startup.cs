// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.AccessTokenServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            // This option is injected as IOptions<RepositoryOptions>.
            builder.Services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    // Set the default to indicate this is an Azure Function.
                    repositoryOptions.IsAzureFunction = true;

                    // Bind any matching configuration settings to corresponding
                    // values in the options.
                    configuration.Bind(repositoryOptions);
                });

            builder.Services.AddSingleton<SendingNotificationDataRepository>();
            builder.Services.AddSingleton<GlobalSendingNotificationDataRepository>();
            builder.Services.AddSingleton<UserDataRepository>();
            builder.Services.AddSingleton<SentNotificationDataRepository>();

            builder.Services.AddSingleton<SendQueue>();
            builder.Services.AddSingleton<DataQueue>();

            builder.Services.AddTransient<GetBotAccessTokenService>();
            builder.Services.AddTransient<CreateUserConversationService>();
            builder.Services.AddTransient<SendNotificationService>();
            builder.Services.AddTransient<DelaySendingNotificationService>();
            builder.Services.AddTransient<ManageNotificationResultService>();
        }
    }
}