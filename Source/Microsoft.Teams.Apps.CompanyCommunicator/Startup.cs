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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
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
            services.AddApplicationInsightsTelemetry();

            // Register authentication services.
            var authenticationOptions = new AuthenticationOptions
            {
                // NOTE: This AzureAd:Instance configuration setting does not need to be
                // overridden by any deployment specific value. It can stay the default value
                // that is set in the project's configuration.
                AzureAd_Instance = this.Configuration.GetValue<string>("AzureAd:Instance"),

                AzureAd_TenantId = this.Configuration.GetValue<string>("AzureAd:TenantId"),
                AzureAd_ClientId = this.Configuration.GetValue<string>("AzureAd:ClientId"),
                AzureAd_ApplicationIdURI = this.Configuration.GetValue<string>("AzureAd:ApplicationIdURI"),

                // NOTE: This AzureAd:ValidIssuers configuration setting does not need to be
                // overridden by any deployment specific value. It can stay the default value
                // that is set in the project's configuration.
                AzureAd_ValidIssuers = this.Configuration.GetValue<string>("AzureAd:ValidIssuers"),

                DisableMustBeValidUpnCheck = this.Configuration.GetValue<bool>("DisableMustBeValidUpnCheck", false),
                ValidUpns = this.Configuration.GetValue<string>("ValidUpns"),
            };
            services.AddOptions<AuthenticationOptions>().Configure(authenticationOptionsToConfigure =>
            {
                authenticationOptionsToConfigure = authenticationOptions;
            });
            services.AddAuthentication(authenticationOptions);

            // Setup MVC.
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Setup SPA static files.
            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Register bot services.
            services.AddOptions<BotOptions>()
                .Configure<IConfiguration>((botOptions, configuration) =>
                {
                    botOptions.MicrosoftAppId = this.Configuration.GetValue<string>("MicrosoftAppId");
                    botOptions.MicrosoftAppPassword = this.Configuration.GetValue<string>("MicrosoftAppPassword");
                });
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddOptions<BotFilterMiddlewareOptions>()
                .Configure<IConfiguration>((botFilterMiddlewareOptions, configuration) =>
                {
                    botFilterMiddlewareOptions.DisableTenantFilter =
                        this.Configuration.GetValue<bool>("DisableTenantFilter", false);
                    botFilterMiddlewareOptions.AllowedTenants =
                        this.Configuration.GetValue<string>("AllowedTenants");
                });
            services.AddSingleton<CompanyCommunicatorBotFilterMiddleware>();
            services.AddSingleton<CompanyCommunicatorBotAdapter>();
            services.AddTransient<TeamsDataCapture>();
            services.AddTransient<IBot, CompanyCommunicatorBot>();

            // Register repository services.
            services.AddOptions<RepositoryOptions>()
                .Configure<IConfiguration>((repositoryOptions, configuration) =>
                {
                    repositoryOptions.StorageAccountConnectionString =
                        this.Configuration.GetValue<string>("StorageAccountConnectionString");

                    // Setting this to false because the main app should ensure that all
                    // tables exist.
                    repositoryOptions.IsItExpectedThatTableAlreadyExists = false;
                });
            services.AddSingleton<TeamDataRepository>();
            services.AddSingleton<UserDataRepository>();
            services.AddSingleton<SentNotificationDataRepository>();
            services.AddTransient<TableRowKeyGenerator>();
            services.AddSingleton<NotificationDataRepository>();

            // Register draft notification preview services.
            services.AddTransient<AdaptiveCardCreator>();
            services.AddTransient<DraftNotificationPreviewService>();

            // Register dependencies for sending a notification.
            services.AddOptions<MessageQueueOptions>()
                .Configure<IConfiguration>((messageQueueOptions, configuration) =>
                {
                    messageQueueOptions.ServiceBusConnection =
                        this.Configuration.GetValue<string>("ServiceBusConnection");
                });
            services.AddSingleton<PrepareToSendQueue>();
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
    }
}
