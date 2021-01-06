// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator
{
    extern alias BetaLib;

    using System.Net;
    using global::Azure.Storage.Blobs;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.DraftNotificationPreview;
    using Microsoft.Teams.Apps.CompanyCommunicator.Localization;

    using Beta = BetaLib::Microsoft.Graph;

    /// <summary>
    /// Register services in DI container, and set up middle-wares in the pipeline.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">IConfiguration instance.</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the IConfiguration instance.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add all options set from configuration values.
            services.AddOptions<AuthenticationOptions>()
                .Configure<IConfiguration>((authenticationOptions, configuration) =>
                {
                    Startup.FillAuthenticationOptionsProperties(authenticationOptions, configuration);
                });
            services.AddOptions<BotOptions>()
                .Configure<IConfiguration>((botOptions, configuration) =>
                {
                    botOptions.UserAppId = configuration.GetValue<string>("UserAppId");
                    botOptions.UserAppPassword = configuration.GetValue<string>("UserAppPassword");
                    botOptions.AuthorAppId = configuration.GetValue<string>("AuthorAppId");
                    botOptions.AuthorAppPassword = configuration.GetValue<string>("AuthorAppPassword");
                });
            services.AddOptions<BotFilterMiddlewareOptions>()
                .Configure<IConfiguration>((botFilterMiddlewareOptions, configuration) =>
                {
                    botFilterMiddlewareOptions.DisableTenantFilter =
                        configuration.GetValue<bool>("DisableTenantFilter", false);
                    botFilterMiddlewareOptions.AllowedTenants =
                        configuration.GetValue<string>("AllowedTenants");
                });
            services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    repositoryOptions.StorageAccountConnectionString =
                        configuration.GetValue<string>("StorageAccountConnectionString");

                    // Setting this to true because the main application should ensure that all
                    // tables exist.
                    repositoryOptions.EnsureTableExists = true;
                });
            services.AddOptions<MessageQueueOptions>()
                .Configure<IConfiguration>((messageQueueOptions, configuration) =>
                {
                    messageQueueOptions.ServiceBusConnection =
                        configuration.GetValue<string>("ServiceBusConnection");
                });
            services.AddOptions<DataQueueMessageOptions>()
                .Configure<IConfiguration>((dataQueueMessageOptions, configuration) =>
                {
                    dataQueueMessageOptions.ForceCompleteMessageDelayInSeconds =
                        configuration.GetValue<double>("ForceCompleteMessageDelayInSeconds", 86400);
                });

            services.AddOptions<UserAppOptions>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    options.ProactivelyInstallUserApp =
                        configuration.GetValue<bool>("ProactivelyInstallUserApp", true);

                    options.UserAppExternalId =
                        configuration.GetValue<string>("UserAppExternalId", "148a66bb-e83d-425a-927d-09f4299a9274");
                });

            services.AddOptions();

            // Add localization services.
            services.AddLocalizationSettings(this.Configuration);

            // Add authentication services.
            AuthenticationOptions authenticationOptionsParameter = new AuthenticationOptions();
            Startup.FillAuthenticationOptionsProperties(authenticationOptionsParameter, this.Configuration);
            services.AddAuthentication(this.Configuration, authenticationOptionsParameter);
            services.AddControllersWithViews();

            // Setup SPA static files.
            // In production, the React files will be served from this directory.
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Add blob client.
            services.AddSingleton(sp => new BlobContainerClient(
                sp.GetService<IConfiguration>().GetValue<string>("StorageAccountConnectionString"),
                Common.Constants.BlobContainerName));

            // The bot needs an HttpClient to download and upload files.
            services.AddHttpClient();

            // Add bot services.
            services.AddTransient<TeamsDataCapture>();
            services.AddTransient<TeamsFileUpload>();
            services.AddTransient<UserTeamsActivityHandler>();
            services.AddTransient<AuthorTeamsActivityHandler>();
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddTransient<CompanyCommunicatorBotFilterMiddleware>();
            services.AddSingleton<CompanyCommunicatorBotAdapter>();
            services.AddSingleton<BotFrameworkHttpAdapter>();

            // Add repositories.
            services.AddSingleton<ITeamDataRepository, TeamDataRepository>();
            services.AddSingleton<IUserDataRepository, UserDataRepository>();
            services.AddSingleton<ISentNotificationDataRepository, SentNotificationDataRepository>();
            services.AddSingleton<INotificationDataRepository, NotificationDataRepository>();
            services.AddSingleton<IExportDataRepository, ExportDataRepository>();
            services.AddSingleton<IAppConfigRepository, AppConfigRepository>();

            // Add service bus message queues.
            services.AddSingleton<IPrepareToSendQueue, PrepareToSendQueue>();
            services.AddSingleton<IDataQueue, DataQueue>();
            services.AddSingleton<IExportQueue, ExportQueue>();

            // Add draft notification preview services.
            services.AddTransient<DraftNotificationPreviewService>();

            // Add microsoft graph services.
            services.AddScoped<IAuthenticationProvider, GraphTokenProvider>();
            services.AddScoped<IGraphServiceClient, GraphServiceClient>();
            services.AddScoped<Beta.IGraphServiceClient, Beta.GraphServiceClient>();
            services.AddScoped<IGraphServiceFactory, GraphServiceFactory>();
            services.AddScoped<IGroupsService>(sp => sp.GetRequiredService<IGraphServiceFactory>().GetGroupsService());
            services.AddScoped<IAppCatalogService>(sp => sp.GetRequiredService<IGraphServiceFactory>().GetAppCatalogService());

            // Add Application Insights telemetry.
            services.AddApplicationInsightsTelemetry();

            // Add miscellaneous dependencies.
            services.AddTransient<TableRowKeyGenerator>();
            services.AddTransient<AdaptiveCardCreator>();
            services.AddTransient<IAppSettingsService, AppSettingsService>();
            services.AddTransient<IUserDataService, UserDataService>();
            services.AddTransient<ITeamMembersService, TeamMembersService>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">IApplicationBuilder instance, which is a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="env">IHostingEnvironment instance, which provides information about the web hosting environment an application is running in.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler(applicationBuilder => this.HandleGlobalException(applicationBuilder));

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLocalization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                   name: "default",
                   pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }

        /// <summary>
        /// Fills the AuthenticationOptions's properties with the correct values from the configuration.
        /// </summary>
        /// <param name="authenticationOptions">The AuthenticationOptions whose properties will be filled.</param>
        /// <param name="configuration">The configuration.</param>
        private static void FillAuthenticationOptionsProperties(AuthenticationOptions authenticationOptions, IConfiguration configuration)
        {
            // NOTE: This AzureAd:Instance configuration setting does not need to be
            // overridden by any deployment specific value. It can stay the default value
            // that is set in the project's configuration.
            authenticationOptions.AzureAdInstance = configuration.GetValue<string>("AzureAd:Instance");

            authenticationOptions.AzureAdTenantId = configuration.GetValue<string>("AzureAd:TenantId");
            authenticationOptions.AzureAdClientId = configuration.GetValue<string>("AzureAd:ClientId");
            authenticationOptions.AzureAdApplicationIdUri = configuration.GetValue<string>("AzureAd:ApplicationIdUri");

            // NOTE: This AzureAd:ValidIssuers configuration setting does not need to be
            // overridden by any deployment specific value. It can stay the default value
            // that is set in the project's configuration.
            authenticationOptions.AzureAdValidIssuers = configuration.GetValue<string>("AzureAd:ValidIssuers");

            authenticationOptions.DisableCreatorUpnCheck = configuration.GetValue<bool>("DisableCreatorUpnCheck", false);
            authenticationOptions.AuthorizedCreatorUpns = configuration.GetValue<string>("AuthorizedCreatorUpns");
        }

        /// <summary>
        /// Handle exceptions happened in the HTTP process pipe-line.
        /// </summary>
        /// <param name="applicationBuilder">IApplicationBuilder instance, which is a class that provides the mechanisms to configure an application's request pipeline.</param>
        private void HandleGlobalException(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    var loggerFactory = applicationBuilder.ApplicationServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(nameof(Startup));
                    logger.LogError($"{contextFeature.Error}");

                    await context.Response.WriteAsync(new
                    {
                        context.Response.StatusCode,
                        Message = "Internal Server Error.",
                    }.ToString());
                }
            });
        }
    }
}
