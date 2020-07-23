// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Export.Func
{
    using System;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph.Users;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Activities;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Orchestrator;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Streams;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Add all options set from configuration values.
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
            builder.Services.AddOptions<MessageQueueOptions>()
                .Configure<IConfiguration>((messageQueueOptions, configuration) =>
                {
                    messageQueueOptions.ServiceBusConnection =
                        configuration.GetValue<string>("ServiceBusConnection");
                });
            builder.Services.AddOptions<ConfidentialClientApplicationOptions>().
         Configure<IConfiguration>((confidentialClientApplicationOptions, configuration) =>
         {
             confidentialClientApplicationOptions.ClientId = configuration.GetValue<string>("ClientId");
             confidentialClientApplicationOptions.ClientSecret = configuration.GetValue<string>("ClientSecret");
             confidentialClientApplicationOptions.TenantId = configuration.GetValue<string>("TenantId");
         });

            builder.Services.AddTransient<ExportOrchestration>();

            // Add activities.
            builder.Services.AddTransient<GetMetaDataActivity>();
            builder.Services.AddTransient<UploadActivity>();
            builder.Services.AddTransient<SendFileCardActivity>();

            // Add bot services.
            builder.Services.AddSingleton<CommonMicrosoftAppCredentials>();
            builder.Services.AddSingleton<ICredentialProvider, CommonBotCredentialProvider>();
            builder.Services.AddSingleton<BotFrameworkHttpAdapter>();

            // Add repositories.
            builder.Services.AddSingleton<SendingNotificationDataRepository>();
            builder.Services.AddSingleton<UserDataRepository>();
            builder.Services.AddSingleton<SentNotificationDataRepository>();
            builder.Services.AddSingleton<ExportDataRepository>();
            builder.Services.AddSingleton<NotificationDataRepository>();
            builder.Services.AddSingleton<TeamDataRepository>();

            // Add service bus message queues.
            builder.Services.AddSingleton<ExportQueue>();

            // Add graph token services
            builder.Services.AddSingleton<IConfidentialClientApplication>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ConfidentialClientApplicationOptions>>();
                return ConfidentialClientApplicationBuilder
                    .Create(options.Value.ClientId)
                    .WithClientSecret(options.Value.ClientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{options.Value.TenantId}"))
                    .Build();
            });
            builder.Services.AddTransient<IGraphServiceClient>(sp => new GraphServiceClient(sp.GetRequiredService<IAuthenticationProvider>()));
            builder.Services.AddTransient<IAuthenticationProvider, MsalAuthenticationProvider>();
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddTransient<IDataStreamFacade, DataStreamFacade>();
        }
    }
}
