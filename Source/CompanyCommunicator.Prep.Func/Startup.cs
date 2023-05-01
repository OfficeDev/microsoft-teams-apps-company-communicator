// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func
{
    using System;
    using System.Globalization;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Blob;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Add all options set from configuration values.
            builder.Services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    repositoryOptions.StorageAccountConnectionString =
                        configuration.GetValue<string>("StorageAccountConnectionString");

                    // Defaulting this value to true because the main app should ensure all
                    // tables exist. It is here as a possible configuration setting in
                    // case it needs to be set differently.
                    repositoryOptions.EnsureTableExists =
                        !configuration.GetValue<bool>("IsItExpectedThatTableAlreadyExists", false);
                });

            builder.Services.AddAppConfiguration(builder.GetContext().Configuration);

            builder.Services.AddOptions<BotOptions>()
                .Configure<IConfiguration>((botOptions, configuration) =>
                {
                    botOptions.UserAppId =
                        configuration.GetValue<string>("UserAppId");
                    botOptions.UserAppPassword =
                        configuration.GetValue<string>("UserAppPassword", string.Empty);
                    botOptions.AuthorAppId =
                        configuration.GetValue<string>("AuthorAppId");
                    botOptions.AuthorAppPassword =
                        configuration.GetValue<string>("AuthorAppPassword", string.Empty);
                    botOptions.GraphAppId =
                        configuration.GetValue<string>("GraphAppId");
                    botOptions.UseCertificate =
                        configuration.GetValue<bool>("UseCertificate", false);
                    botOptions.AuthorAppCertName =
                        configuration.GetValue<string>("AuthorAppCertName", string.Empty);
                    botOptions.UserAppCertName =
                        configuration.GetValue<string>("UserAppCertName", string.Empty);
                    botOptions.GraphAppCertName =
                        configuration.GetValue<string>("GraphAppCertName", string.Empty);
                });
            builder.Services.AddOptions<DataQueueMessageOptions>()
                .Configure<IConfiguration>((dataQueueMessageOptions, configuration) =>
                {
                    dataQueueMessageOptions.MessageDelayInSeconds =
                        configuration.GetValue<int>("DataQueueMessageDelayInSeconds", 5);
                });

            builder.Services.AddOptions<TeamsConversationOptions>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    options.ProactivelyInstallUserApp =
                        configuration.GetValue<bool>("ProactivelyInstallUserApp", true);

                    options.MaxAttemptsToCreateConversation =
                        configuration.GetValue<int>("MaxAttemptsToCreateConversation", 2);
                });

            builder.Services.AddLocalization();

            var useManagedIdentity = bool.Parse(Environment.GetEnvironmentVariable("UseManagedIdentity"));
            builder.Services.AddBlobClient(useManagedIdentity);
            builder.Services.AddServiceBusClient(useManagedIdentity);

            // Set current culture.
            var culture = Environment.GetEnvironmentVariable("i18n:DefaultCulture");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);

            // Add bot services.
            builder.Services.AddSingleton<UserAppCredentials>();
            builder.Services.AddSingleton<AuthorAppCredentials>();
            builder.Services.AddSingleton<ServiceClientCredentialsFactory, ConfigurationCredentialProvider>();
            builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
            builder.Services.AddSingleton<CCBotAdapterBase, CCBotAdapter>();

            // Add repositories.
            builder.Services.AddSingleton<INotificationDataRepository, NotificationDataRepository>();
            builder.Services.AddSingleton<ISendingNotificationDataRepository, SendingNotificationDataRepository>();
            builder.Services.AddSingleton<ISentNotificationDataRepository, SentNotificationDataRepository>();
            builder.Services.AddSingleton<IUserDataRepository, UserDataRepository>();
            builder.Services.AddSingleton<ITeamDataRepository, TeamDataRepository>();
            builder.Services.AddSingleton<IExportDataRepository, ExportDataRepository>();
            builder.Services.AddSingleton<IAppConfigRepository, AppConfigRepository>();

            // Add service bus message queues.
            builder.Services.AddSingleton<ISendQueue, SendQueue>();
            builder.Services.AddSingleton<IDataQueue, DataQueue>();
            builder.Services.AddSingleton<IExportQueue, ExportQueue>();

            // Add miscellaneous dependencies.
            builder.Services.AddTransient<TableRowKeyGenerator>();
            builder.Services.AddTransient<AdaptiveCardCreator>();
            builder.Services.AddTransient<IAppSettingsService, AppSettingsService>();
            builder.Services.AddTransient<IStorageClientFactory, StorageClientFactory>();
            builder.Services.AddTransient<IUserTypeService, UserTypeService>();
            builder.Services.AddTransient<IRecipientsService, RecipientsService>();
            builder.Services.AddTransient<IStorageClientFactory, StorageClientFactory>();
            builder.Services.AddTransient<IBlobStorageProvider, BlobStorageProvider>();

            // Add Teams services.
            builder.Services.AddTransient<ITeamMembersService, TeamMembersService>();
            builder.Services.AddTransient<IConversationService, ConversationService>();

            // Add Secrets.
            var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVault:Url");
            builder.Services.AddSecretsProvider(keyVaultUrl);

            // Add graph services.
            this.AddGraphServices(builder);

            builder.Services.AddTransient<IDataStreamFacade, DataStreamFacade>();
        }

        /// <summary>
        /// Adds Graph Services and related dependencies.
        /// </summary>
        /// <param name="builder">Builder.</param>
        private void AddGraphServices(IFunctionsHostBuilder builder)
        {
            // Options
            builder.Services.AddOptions<ConfidentialClientApplicationOptions>().
                Configure<IConfiguration>((confidentialClientApplicationOptions, configuration) =>
                {
                    confidentialClientApplicationOptions.AzureCloudInstance = configuration.GetAzureCloudInstance();
                    confidentialClientApplicationOptions.ClientId = configuration.GetValue<string>("GraphAppId");
                    confidentialClientApplicationOptions.ClientSecret = configuration.GetValue<string>("GraphAppPassword", string.Empty);
                    confidentialClientApplicationOptions.TenantId = configuration.GetValue<string>("TenantId");
                });

            // Graph Token Services
            var useClientCertificates = bool.Parse(Environment.GetEnvironmentVariable("UseCertificate") ?? "false");

            builder.Services.AddConfidentialClient(useClientCertificates);

            builder.Services.AddSingleton<IAuthenticationProvider, MsalAuthenticationProvider>();

            // Add Graph Clients.
            builder.Services.AddSingleton<IGraphServiceClient>(
                serviceProvider =>
                new GraphServiceClient(
                    serviceProvider.GetRequiredService<IAppConfiguration>().GraphBaseUrl,
                    serviceProvider.GetRequiredService<IAuthenticationProvider>()));

            // Add Service Factory
            builder.Services.AddSingleton<IGraphServiceFactory, GraphServiceFactory>();

            // Add Graph Services
            builder.Services.AddScoped<IUsersService>(sp => sp.GetRequiredService<IGraphServiceFactory>().GetUsersService());
            builder.Services.AddScoped<IGroupMembersService>(sp => sp.GetRequiredService<IGraphServiceFactory>().GetGroupMembersService());
            builder.Services.AddScoped<IAppManagerService>(sp => sp.GetRequiredService<IGraphServiceFactory>().GetAppManagerService());
            builder.Services.AddScoped<IChatsService>(sp => sp.GetRequiredService<IGraphServiceFactory>().GetChatsService());
        }
    }
}
