// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Globalization;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Blob;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Add all options set from configuration values.
            builder.Services.AddOptions<SendFunctionOptions>()
                .Configure<IConfiguration>((companyCommunicatorSendFunctionOptions, configuration) =>
                {
                    companyCommunicatorSendFunctionOptions.MaxNumberOfAttempts =
                        configuration.GetValue<int>("MaxNumberOfAttempts", 1);

                    companyCommunicatorSendFunctionOptions.SendRetryDelayNumberOfSeconds =
                        configuration.GetValue<double>("SendRetryDelayNumberOfSeconds", 660);
                });
            builder.Services.AddOptions<BotOptions>()
                .Configure<IConfiguration>((botOptions, configuration) =>
                {
                    botOptions.UserAppId = configuration.GetValue<string>("UserAppId");
                    botOptions.UserAppPassword = configuration.GetValue<string>("UserAppPassword", string.Empty);
                    botOptions.UserAppCertName = configuration.GetValue<string>("UserAppCertName", string.Empty);
                    botOptions.AuthorAppId = configuration.GetValue<string>("AuthorAppId");
                    botOptions.AuthorAppCertName = configuration.GetValue<string>("AuthorAppCertName", string.Empty);
                    botOptions.GraphAppId = configuration.GetValue<string>("GraphAppId");
                    botOptions.GraphAppCertName = configuration.GetValue<string>("GraphAppCertName", string.Empty);
                    botOptions.UseCertificate = configuration.GetValue<bool>("UseCertificate", false);
                });
            builder.Services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    repositoryOptions.StorageAccountConnectionString =
                        configuration.GetValue<string>("StorageAccountConnectionString");

                    // Defaulting this value to true because the main app should ensure all
                    // tables exist. It is here as a possible configuration setting in
                    // case it needs to be set differently.
                    repositoryOptions.EnsureTableExists =
                        !configuration.GetValue<bool>("IsItExpectedThatTableAlreadyExists", true);
                });

            builder.Services.AddLocalization();

            var useManagedIdentity = bool.Parse(Environment.GetEnvironmentVariable("UseManagedIdentity"));
            builder.Services.AddServiceBusClient(useManagedIdentity);

            // Set current culture.
            var culture = Environment.GetEnvironmentVariable("i18n:DefaultCulture");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);

            // Add bot services.
            builder.Services.AddSingleton<UserAppCredentials>();
            builder.Services.AddSingleton<ServiceClientCredentialsFactory, ConfigurationCredentialProvider>();
            builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
            builder.Services.AddSingleton<CCBotAdapterBase, CCBotAdapter>();

            // Add teams services.
            builder.Services.AddTransient<IMessageService, MessageService>();

            // Add repositories.
            builder.Services.AddSingleton<ISendingNotificationDataRepository, SendingNotificationDataRepository>();
            builder.Services.AddSingleton<IGlobalSendingNotificationDataRepository, GlobalSendingNotificationDataRepository>();
            builder.Services.AddSingleton<ISentNotificationDataRepository, SentNotificationDataRepository>();
            builder.Services.AddSingleton<INotificationDataRepository, NotificationDataRepository>();

            // Add service bus message queues.
            builder.Services.AddSingleton<ISendQueue, SendQueue>();

            // Add the Notification service.
            builder.Services.AddTransient<INotificationService, NotificationService>();
            builder.Services.AddTransient<IStorageClientFactory, StorageClientFactory>();
            builder.Services.AddTransient<IBlobStorageProvider, BlobStorageProvider>();

            // Add the cache services
            builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

            // Add Secrets.
            var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVault:Url");
            builder.Services.AddSecretsProvider(keyVaultUrl);
        }
    }
}
