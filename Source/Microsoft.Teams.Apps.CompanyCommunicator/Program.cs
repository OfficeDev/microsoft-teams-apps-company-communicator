// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// Program class of the company communicator application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main function of the company communicator application.
        /// It builds a web host, then launches the company communicator into it.
        /// </summary>
        /// <param name="args">Arguments passed in to the function.</param>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create the web host builder.
        /// </summary>
        /// <param name="args">Arguments passed into the main function.</param>
        /// <returns>A web host builder instance.</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}