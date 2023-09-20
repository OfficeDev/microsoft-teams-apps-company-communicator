﻿// <copyright file="ServiceCollectionExtension.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using global::Azure.Core;
    using global::Azure.Identity;
    using global::Azure.Messaging.ServiceBus;
    using global::Azure.Storage.Blobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// Extension class for registering resources in DI container.
    /// </summary>
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Add Blob client dependency either using Managed identity or connection string.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="useManagedIdentity">boolean to indicate to use managed identity or connection string.</param>
        public static void AddBlobClient(this IServiceCollection services, bool useManagedIdentity)
        {
            // Setup blob client options.
            var options = new BlobClientOptions();

            // configure retries
            options.Retry.MaxRetries = 5; // default is 3
            options.Retry.Mode = RetryMode.Exponential; // default is fixed retry policy
            options.Retry.Delay = TimeSpan.FromSeconds(1); // default is 0.8s

            if (useManagedIdentity)
            {
                // Add using managed identities.
                services.AddSingleton(sp => new BlobContainerClient(
                   GetBlobContainerUri(sp.GetService<IConfiguration>().GetValue<string>("TeamsEnvironment", "Commerical"/*default*/), sp.GetService<IConfiguration>().GetValue<string>("StorageAccountName")),
                   new DefaultAzureCredential(),
                   options));
            }
            else
            {
                // Add using connection strings.
                services.AddSingleton(sp => new BlobContainerClient(
                sp.GetService<IConfiguration>().GetValue<string>("StorageAccountConnectionString"),
                Common.Constants.BlobContainerName,
                options));
            }
        }

        /// <summary>
        /// Add Service Bus client dependency either using Managed identity or connection string.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="useManagedIdentity">boolean to indicate to use managed identity or connection string.</param>
        public static void AddServiceBusClient(this IServiceCollection services, bool useManagedIdentity)
        {
            if (useManagedIdentity)
            {
                // Adding using managed identities.
                services.AddSingleton(sp => new ServiceBusClient(
                sp.GetService<IConfiguration>().GetValue<string>("ServiceBusNamespace"),
                new DefaultAzureCredential()));
            }
            else
            {
                // Adding using connection strings.
                services.AddSingleton(sp => new ServiceBusClient(
                sp.GetService<IConfiguration>().GetValue<string>("ServiceBusConnection")));
            }
        }

        /// <summary>
        /// Add Confidential client dependency to make graph calls.
        /// </summary>
        /// <param name="services">Collection of services.</param>
        /// <param name="useCertificates">boolean to indicate to use certificates or credentials.</param>
        public static void AddConfidentialClient(this IServiceCollection services, bool useCertificates)
        {
            if (useCertificates)
            {
                services.AddSingleton<IConfidentialClientApplication>(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<ConfidentialClientApplicationOptions>>();
                    var certificateProvider = provider.GetRequiredService<ICertificateProvider>();
                    var appConfiguration = provider.GetRequiredService<IAppConfiguration>();
                    var cert = certificateProvider.GetCertificateAsync(options.Value.ClientId).Result;
                    return ConfidentialClientApplicationBuilder
                        .Create(options.Value.ClientId)
                        .WithCertificate(cert)
                        .WithAuthority(new Uri(appConfiguration.AuthorityUri))
                        .Build();
                });
            }
            else
            {
                services.AddSingleton<IConfidentialClientApplication>(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<ConfidentialClientApplicationOptions>>();
                    var appConfiguration = provider.GetRequiredService<IAppConfiguration>();
                    return ConfidentialClientApplicationBuilder
                        .Create(options.Value.ClientId)
                        .WithClientSecret(options.Value.ClientSecret)
                        .WithAuthority(new Uri(appConfiguration.AuthorityUri))
                        .Build();
                });
            }
        }

        /// <summary>
        /// Adds relevant App configurations for Teams environment.
        /// </summary>
        /// <param name="services">Serivce collection.</param>
        /// <param name="configuration">Configuration.</param>
        public static void AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var tenantId = configuration.GetValue<string>("TenantId") ?? configuration.GetValue<string>("AzureAd:TenantId");
            var env = configuration.GetTeamsEnvironment();
            services.AddSingleton<IAppConfiguration>(new ConfigurationFactory(tenantId).GetAppConfiguration(env));
        }

        public static AzureCloudInstance GetAzureCloudInstance(this IConfiguration configuration)
        {
            var teamsEnv = configuration.GetTeamsEnvironment();
            switch (teamsEnv)
            {
                case TeamsEnvironment.Commercial:
                case TeamsEnvironment.GCC:
                    return AzureCloudInstance.AzurePublic;
                case TeamsEnvironment.GCCH:
                    return AzureCloudInstance.AzureUsGovernment;
                case TeamsEnvironment.DOD:
                    return AzureCloudInstance.AzureUsGovernment;
                default:
                    return AzureCloudInstance.AzurePublic;
            }
        }

        /// <summary>
        /// Reads Teams environment from the configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <returns>Teams environemnt.</returns>
        public static TeamsEnvironment GetTeamsEnvironment(this IConfiguration configuration)
        {
            var envString = configuration.GetValue<string>("TeamsEnvironment", "Commerical"/*default*/);
            Enum.TryParse(envString, out TeamsEnvironment teamsEnvironment);
            return teamsEnvironment;
        }

        private static Uri GetBlobContainerUri(string envString, string storageAccountName)
        {
            Enum.TryParse(envString, out TeamsEnvironment teamsEnvironment);
            switch (teamsEnvironment)
            {
                case TeamsEnvironment.Commercial:
                case TeamsEnvironment.GCC:
                    return new Uri(string.Format(
                    "https://{0}.blob.core.windows.net/{1}",
                    storageAccountName,
                    Common.Constants.BlobContainerName));
                case TeamsEnvironment.GCCH:
                case TeamsEnvironment.DOD:
                    return new Uri(string.Format(
                    "https://{0}.blob.core.usgovcloudapi.net/{1}",
                    storageAccountName,
                    Common.Constants.BlobContainerName));
                default:
                    return new Uri(string.Format(
                    "https://{0}.blob.core.windows.net/{1}",
                    storageAccountName,
                    Common.Constants.BlobContainerName));
            }
        }
    }
}
