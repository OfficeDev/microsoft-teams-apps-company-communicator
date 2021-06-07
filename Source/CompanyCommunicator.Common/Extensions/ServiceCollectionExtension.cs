// <copyright file="ServiceCollectionExtension.cs" company="Microsoft">
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
                   GetBlobContainerUri(sp.GetService<IConfiguration>().GetValue<string>("StorageAccountName")),
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

        private static Uri GetBlobContainerUri(string storageAccountName)
        {
            return new Uri(string.Format(
                    "https://{0}.blob.core.windows.net/{1}",
                    storageAccountName,
                    Common.Constants.BlobContainerName));
        }
    }
}
