// <copyright file="AuthenticationServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using System;
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
        private static void RegisterAuthenticationServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options => { options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; })
                .AddJwtBearer(options =>
                {
                    var azureADOptions = new AzureADOptions();
                    configuration.Bind("AzureAd", azureADOptions);
                    options.Authority = $"{azureADOptions.Instance}{azureADOptions.TenantId}/v2.0";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = $"{azureADOptions.ClientId}",
                        ValidIssuer = $"{azureADOptions.Instance}{azureADOptions.TenantId}/v2.0",
                        IssuerValidator = AuthenticationServiceCollectionExtensions.IssuerValidator,
                    };
                });
        }

        private static string IssuerValidator(
            string issuer,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            var validIssuer = validationParameters?.ValidIssuer;
            if (!string.IsNullOrWhiteSpace(validIssuer)
                && validIssuer.Equals(issuer, StringComparison.OrdinalIgnoreCase))
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