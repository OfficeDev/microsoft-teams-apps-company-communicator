// <copyright file="AuthServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Auth
{
    using Microsoft.AspNetCore.Authentication.AzureAD.UI;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension class for registering auth services in DI container.
    /// </summary>
    public static class AuthServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register the auth services.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        /// <param name="configuration">IConfiguration instance.</param>
        public static void AddAuth(this IServiceCollection services, IConfiguration configuration)
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
                    var azureadoptions = new AzureADOptions();
                    configuration.Bind("AzureAd", azureadoptions);
                    options.Authority = $"{azureadoptions.Instance}{azureadoptions.TenantId}/v2.0";
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidAudience = $"{azureadoptions.ClientId}",
                        ValidIssuer = $"{azureadoptions.Instance}{azureadoptions.TenantId}/v2.0",
                    };
                });
        }

        private static void RegisterAuthorizationPolicy(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                var mustContainUpnClaimRequirement = new MustContainUpnClaimRequirement();
                options.AddPolicy(
                    PolicyNames.MustHaveUpnClaimPolicy,
                    policyBuilder => policyBuilder.AddRequirements(mustContainUpnClaimRequirement));
            });

            services.AddSingleton<IAuthorizationHandler, MustContainUpnHandler>();
        }
    }
}
