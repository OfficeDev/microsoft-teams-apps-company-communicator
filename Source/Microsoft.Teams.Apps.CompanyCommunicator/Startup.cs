// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Microsoft.Teams.Apps.CompanyCommunicator.DraftNotificationPreview;

    /// <summary>
    /// Register services in DI container, and set up middlewares in the pipeline.
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
                    botOptions.MicrosoftAppId = configuration.GetValue<string>("MicrosoftAppId");
                    botOptions.MicrosoftAppPassword = configuration.GetValue<string>("MicrosoftAppPassword");
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

                    // Setting this to false because the main app should ensure that all
                    // tables exist.
                    repositoryOptions.IsItExpectedThatTableAlreadyExists = false;
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

            // Add authentication services.
            AuthenticationOptions authenticationOptionsParameter = new AuthenticationOptions();
            Startup.FillAuthenticationOptionsProperties(authenticationOptionsParameter, this.Configuration);

            services.AddAuthentication(authenticationOptionsParameter);

            // Setup MVC.
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Setup SPA static files.
            // In production, the React files will be served from this directory.
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Add bot services.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddTransient<CompanyCommunicatorBotFilterMiddleware>();
            services.AddSingleton<CompanyCommunicatorBotAdapter>();
            services.AddTransient<TeamsDataCapture>();
            services.AddTransient<IBot, CompanyCommunicatorBot>();

            // Add repositories.
            services.AddSingleton<TeamDataRepository>();
            services.AddSingleton<UserDataRepository>();
            services.AddSingleton<SentNotificationDataRepository>();
            services.AddSingleton<NotificationDataRepository>();

            // Add service bus message queues.
            services.AddSingleton<PrepareToSendQueue>();
            services.AddSingleton<DataQueue>();

            // Add draft notification preview services.
            services.AddTransient<DraftNotificationPreviewService>();

            // Add Application Insights telemetry.
            services.AddApplicationInsightsTelemetry();

            // Add miscellaneous dependencies.
            services.AddTransient<TableRowKeyGenerator>();
            services.AddTransient<AdaptiveCardCreator>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">IApplicationBuilder instance, which is a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="env">IHostingEnvironment instance, which provides information about the web hosting environment an application is running in.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
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
    }
}
