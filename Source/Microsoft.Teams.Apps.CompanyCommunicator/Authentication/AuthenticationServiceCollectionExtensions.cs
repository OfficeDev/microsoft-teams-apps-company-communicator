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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Extension class for registering auth services in DI container.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register the authentication services.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        /// <param name="authenticationOptions">The authentication options.</param>
        public static void AddAuthentication(
            this IServiceCollection services,
            AuthenticationOptions authenticationOptions)
        {
            AuthenticationServiceCollectionExtensions.RegisterAuthenticationServices(services, authenticationOptions);

            AuthenticationServiceCollectionExtensions.RegisterAuthorizationPolicy(services);
        }

        // This method works specifically for single tenant application.
        private static void RegisterAuthenticationServices(
            IServiceCollection services,
            AuthenticationOptions authenticationOptions)
        {
            AuthenticationServiceCollectionExtensions.ValidateAuthenticationOptions(authenticationOptions);

            services.AddAuthentication(options => { options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; })
                .AddJwtBearer(options =>
                {
                    var azureADOptions = new AzureADOptions
                    {
                        Instance = authenticationOptions.AzureAd_Instance,
                        TenantId = authenticationOptions.AzureAd_TenantId,
                        ClientId = authenticationOptions.AzureAd_ClientId,
                    };

                    options.Authority = $"{azureADOptions.Instance}{azureADOptions.TenantId}/v2.0";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences = AuthenticationServiceCollectionExtensions.GetValidAudiences(authenticationOptions),
                        ValidIssuers = AuthenticationServiceCollectionExtensions.GetValidIssuers(authenticationOptions),
                        AudienceValidator = AuthenticationServiceCollectionExtensions.AudienceValidator,
                    };
                });
        }

        private static void ValidateAuthenticationOptions(AuthenticationOptions authenticationOptions)
        {
            if (string.IsNullOrWhiteSpace(authenticationOptions?.AzureAd_ClientId))
            {
                throw new ApplicationException("AzureAd ClientId is missing in the configuration file.");
            }

            if (string.IsNullOrWhiteSpace(authenticationOptions?.AzureAd_TenantId))
            {
                throw new ApplicationException("AzureAd TenantId is missing in the configuration file.");
            }

            if (string.IsNullOrWhiteSpace(authenticationOptions?.AzureAd_ApplicationIdURI))
            {
                throw new ApplicationException("AzureAd ApplicationIdURI is missing in the configuration file.");
            }

            if (string.IsNullOrWhiteSpace(authenticationOptions?.AzureAd_ValidIssuers))
            {
                throw new ApplicationException("AzureAd ValidIssuers is missing in the configuration file.");
            }
        }

        private static IEnumerable<string> SplitAuthenticationOptionsList(string stringInAuthenticationOptions)
        {
            var settings = stringInAuthenticationOptions
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim());
            if (settings == null)
            {
                throw new ApplicationException($"Invalid list of settings in authenticaiton options.");
            }

            return settings;
        }

        private static IEnumerable<string> GetValidAudiences(AuthenticationOptions authenticationOptions)
        {
            var validAudiences = new List<string>
            {
                authenticationOptions.AzureAd_ClientId,
                authenticationOptions.AzureAd_ApplicationIdURI.ToLower(),
            };

            return validAudiences;
        }

        private static IEnumerable<string> GetValidIssuers(AuthenticationOptions authenticationOptions)
        {
            var tenantId = authenticationOptions.AzureAd_TenantId;

            var validIssuers =
                AuthenticationServiceCollectionExtensions.SplitAuthenticationOptionsList(
                    authenticationOptions.AzureAd_ValidIssuers);

            validIssuers = validIssuers.Select(validIssuer => validIssuer.Replace("TENANT_ID", tenantId));

            return validIssuers;
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

        private static bool AudienceValidator(
            IEnumerable<string> tokenAudiences,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            if (tokenAudiences == null || tokenAudiences.Count() == 0)
            {
                throw new ApplicationException("No audience defined in token!");
            }

            var validAudiences = validationParameters.ValidAudiences;
            if (validAudiences == null || validAudiences.Count() == 0)
            {
                throw new ApplicationException("No valid audiences defined in validationParameters!");
            }

            foreach (var tokenAudience in tokenAudiences)
            {
                if (validAudiences.Any(validAudience => validAudience.Equals(tokenAudience, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
