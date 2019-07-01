// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;

    /// <summary>
    /// Register services in DI container, and set up middlewares in the pipeline.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Register bot services in DI container
            services.AddBot();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">IApplicationBuilder instance, which is a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="env">IHostingEnvironment instance, which Provides information about the web hosting environment an application is running in.</param>
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
