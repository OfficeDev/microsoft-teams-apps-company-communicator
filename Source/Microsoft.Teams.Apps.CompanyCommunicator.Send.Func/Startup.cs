// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.DataServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Add all options set from configuration values.
            builder.Services.AddOptions<CompanyCommunicatorSendFunctionOptions>()
                .Configure<IConfiguration>((companyCommunicatorSendFunctionOptions, configuration) =>
                {
                    companyCommunicatorSendFunctionOptions.MaxNumberOfAttempts =
                        configuration.GetValue<int>("MaxNumberOfAttempts", 1);

                    companyCommunicatorSendFunctionOptions.SendRetryDelayNumberOfMinutes =
                        configuration.GetValue<int>("SendRetryDelayNumberOfMinutes", 11);
                });
            builder.Services.AddOptions<BotOptions>()
                .Configure<IConfiguration>((botOptions, configuration) =>
                {
                    botOptions.MicrosoftAppId =
                        configuration.GetValue<string>("MicrosoftAppId");

                    botOptions.MicrosoftAppPassword =
                        configuration.GetValue<string>("MicrosoftAppPassword");
                });
            builder.Services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    repositoryOptions.StorageAccountConnectionString =
                        configuration.GetValue<string>("StorageAccountConnectionString");

                    // Defaulting this value to true because the main app should ensure all
                    // tables exist. It is here as a possible configuration setting in
                    // case it needs to be set differently.
                    repositoryOptions.IsItExpectedThatTableAlreadyExists =
                        configuration.GetValue<bool>("IsItExpectedThatTableAlreadyExists", true);
                });

            // Add the create user conversation service.
            builder.Services.AddTransient<CreateUserConversationService>();

            // Add the notification services.
            builder.Services.AddTransient<SendNotificationService>();
            builder.Services.AddTransient<DelaySendingNotificationService>();

            // Add the result data service.
            builder.Services.AddTransient<ManageResultDataService>();

            // Add bot services.
            builder.Services.AddSingleton<CommonMicrosoftAppCredentials>();
            builder.Services.AddSingleton<ICredentialProvider, CommonBotCredentialProvider>();
            builder.Services.AddSingleton<CommonBotAdapter>();

            // Add repositories.
            builder.Services.AddSingleton<SendingNotificationDataRepository>();
            builder.Services.AddSingleton<GlobalSendingNotificationDataRepository>();
            builder.Services.AddSingleton<UserDataRepository>();
            builder.Services.AddSingleton<SentNotificationDataRepository>();

            // Add service bus message queues.
            builder.Services.AddSingleton<SendQueue>();
            builder.Services.AddSingleton<DataQueue>();
        }
    }
}
