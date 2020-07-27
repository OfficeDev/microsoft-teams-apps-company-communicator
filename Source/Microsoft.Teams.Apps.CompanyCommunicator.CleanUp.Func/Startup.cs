// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.CleanUp.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.CleanUp.Func
{
    using global::Azure.Storage.Blobs;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.CleanUp.Func.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

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
            builder.Services.AddOptions<CleanUpFileOptions>()
                   .Configure<IConfiguration>((cleanUpFileOptions, configuration) =>
                   {
                       cleanUpFileOptions.CleanUpFile =
                           configuration.GetValue<string>("CleanUpFile");
                   });

            builder.Services.AddOptions<RepositoryOptions>()
                    .Configure<IConfiguration>((repositoryOptions, configuration) =>
                    {
                        repositoryOptions.StorageAccountConnectionString =
                            configuration.GetValue<string>("StorageAccountConnectionString");

                        // Setting this to false because the main app should ensure that all
                        // tables exist.
                        repositoryOptions.IsItExpectedThatTableAlreadyExists = false;
                    });

            // Add blob client.
            builder.Services.AddSingleton(sp => new BlobContainerClient(
                sp.GetService<IConfiguration>().GetValue<string>("StorageAccountConnectionString"),
                Common.Constants.BlobContainerName));

            // Add bot services.
            builder.Services.AddSingleton<CommonMicrosoftAppCredentials>();
            builder.Services.AddSingleton<ICredentialProvider, CommonBotCredentialProvider>();
            builder.Services.AddSingleton<BotFrameworkHttpAdapter>();

            // Add repositories.
            builder.Services.AddSingleton<UserDataRepository>();
            builder.Services.AddSingleton<ExportDataRepository>();

            // Add services.
            builder.Services.AddSingleton<IFileCardService, FileCardService>();
        }
    }
}
