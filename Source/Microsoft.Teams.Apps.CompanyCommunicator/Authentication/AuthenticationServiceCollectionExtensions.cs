// <copyright file="AuthenticationServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Authentication.AzureAD.UI;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Extension class for registering auth services in DI container.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        private static readonly string ClientIdConfigurationSettingKey = "AzureAd:ClientId";
        private static readonly string TenantIdConfigurationSettingKey = "AzureAd:TenantId";
        private static readonly string ValidAudiencesConfigurationSettingKey = "AzureAd:ValidAudiences";
        private static readonly string ValidIssuersConfigurationSettingKey = "AzureAd:ValidIssuers";

        /// <summary>
        /// Extension method to register the authentication services.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        /// <param name="configuration">IConfiguration instance.</param>
        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            RegisterAuthenticationServices(services, configuration);

            RegisterAuthorizationPolicy(services);
        }

        // This method works specifically for single tenant application.
        private static void RegisterAuthenticationServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            AuthenticationServiceCollectionExtensions.ValidateAuthenticationConfigurationSettings(configuration);

            services.AddAuthentication(options => { options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; })
                .AddJwtBearer(options =>
                {
                    var azureADOptions = new AzureADOptions();
                    configuration.Bind("AzureAd", azureADOptions);
                    options.Authority = $"{azureADOptions.Instance}{azureADOptions.TenantId}/v2.0";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences = AuthenticationServiceCollectionExtensions.GetValidAudiences(configuration),
                        ValidIssuers = AuthenticationServiceCollectionExtensions.GetValidIssuers(configuration),
                        IssuerValidator = AuthenticationServiceCollectionExtensions.IssuerValidator,
                    };
                });
        }

        private static void ValidateAuthenticationConfigurationSettings(IConfiguration configuration)
        {
            var clientId = configuration[AuthenticationServiceCollectionExtensions.ClientIdConfigurationSettingKey];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ApplicationException("Azure AD ClientId is missing in configuration file.");
            }

            var tenantId = configuration[AuthenticationServiceCollectionExtensions.TenantIdConfigurationSettingKey];
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ApplicationException("Azure AD TenantId is missing in configuration file.");
            }

            var validAudiences = configuration[AuthenticationServiceCollectionExtensions.ValidAudiencesConfigurationSettingKey];
            if (string.IsNullOrWhiteSpace(validAudiences))
            {
                throw new ApplicationException("Azure AD ValidAudiences is missing in configuration file.");
            }

            var validIssuers = configuration[AuthenticationServiceCollectionExtensions.ValidIssuersConfigurationSettingKey];
            if (string.IsNullOrWhiteSpace(validIssuers))
            {
                throw new ApplicationException("Azure AD ValidIssuers is missing in configuration file.");
            }
        }

        private static IEnumerable<string> GetSettings(IConfiguration configuration, string configurationSettingKey)
        {
            var configurationSettingValue = configuration[configurationSettingKey];
            var settings = configurationSettingValue
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim());
            if (settings == null)
            {
                throw new ApplicationException($"{configurationSettingKey} doesn't contain valid settings in configuration.");
            }

            return settings;
        }

        private static IEnumerable<string> GetValidAudiences(IConfiguration configuration)
        {
            var clientId = configuration[AuthenticationServiceCollectionExtensions.ClientIdConfigurationSettingKey];

            var validAudiences = new List<string> { clientId };

            var configurationSettings =
                AuthenticationServiceCollectionExtensions.GetSettings(
                    configuration,
                    AuthenticationServiceCollectionExtensions.ValidAudiencesConfigurationSettingKey);

            validAudiences.AddRange(configurationSettings);

            return validAudiences;
        }

        private static IEnumerable<string> GetValidIssuers(IConfiguration configuration)
        {
            var tenantId = configuration[AuthenticationServiceCollectionExtensions.TenantIdConfigurationSettingKey];

            var validIssuers =
                AuthenticationServiceCollectionExtensions.GetSettings(
                    configuration,
                    AuthenticationServiceCollectionExtensions.ValidIssuersConfigurationSettingKey);

            validIssuers = validIssuers.Select(validIssuer => validIssuer.Replace("TENANT_ID", tenantId));

            return validIssuers;
        }

        private static string IssuerValidator(
            string issuer,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            var validIssuers = validationParameters?.ValidIssuers;
            if (validIssuers != null &&
                validIssuers.Any(validIssuer => issuer.Equals(validIssuer, StringComparison.OrdinalIgnoreCase)))
            {
                return issuer;
            }

            throw new ApplicationException("Invalid issuer!");
        }

        private static void RegisterAuthorizationPolicy(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                var mustContainUpnClaimRequirement = new MustBeValidUpnRequirement();
                options.AddPolicy(
                    PolicyNames.MustBeValidUpnPolicy,
                    policyBuilder => policyBuilder.AddRequirements(mustContainUpnClaimRequirement));
            });

            services.AddSingleton<IAuthorizationHandler, MustBeValidUpnHandler>();
        }
    }
}