// <copyright file="SecretsRegistry.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets
{
    using System;
    using global::Azure.Core;
    using global::Azure.Identity;
    using global::Azure.Security.KeyVault.Certificates;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Register secrets dependencies.
    /// </summary>
    public static class SecretsRegistry
    {
        /// <summary>
        /// Service Collection extension.
        ///
        /// Injects secrets provider.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="keyVaultUrl">Key vault url.</param>
        /// <returns>the service collection.</returns>
        public static IServiceCollection AddSecretsProvider(this IServiceCollection services, string keyVaultUrl)
        {
            if (string.IsNullOrEmpty(keyVaultUrl))
            {
                throw new ArgumentNullException("KeyVault Url is null or empty.");
            }

            var options = new CertificateClientOptions();
            options.AddPolicy(new KeyVaultProxy(), HttpPipelinePosition.PerCall);
            services.AddSingleton(new CertificateClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options));
            services.AddSingleton<ICertificateProvider, CertificateProvider>();

            return services;
        }
    }
}
