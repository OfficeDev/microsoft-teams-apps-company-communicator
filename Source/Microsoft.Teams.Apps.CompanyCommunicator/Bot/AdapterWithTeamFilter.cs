// <copyright file="AdapterWithTeamFilter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Linq;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Teams.Middlewares;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Bot adapter with teams filter.
    /// </summary>
    public class AdapterWithTeamFilter : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterWithTeamFilter"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        /// <param name="credentialProvider">Credential provider serive instance.</param>
        /// <param name="teamFilterMiddleware">Channel filter (channelId=mstests) middleware instance.</param>
        public AdapterWithTeamFilter(
            IConfiguration configuration,
            ICredentialProvider credentialProvider,
            TeamFilterMiddleware teamFilterMiddleware)
            : base(credentialProvider)
        {
            this.Use(teamFilterMiddleware);

            this.UseTenantFilterMiddlewareIfEnabled(configuration);
        }

        private void UseTenantFilterMiddlewareIfEnabled(IConfiguration configuration)
        {
            var disableTenantFilter = configuration.GetValue<bool>("DisableTenantFilter", false);
            if (disableTenantFilter)
            {
                return;
            }

            var allowedTenants = configuration
                ?.GetValue<string>("AllowedTenants", string.Empty)
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim());
            if (allowedTenants == null || allowedTenants.Count() == 0)
            {
                var exceptionMessage = "AllowedTenants setting is not set properly in the configuration file.");
                Console.WriteLine(exceptionMessage);
                throw new ApplicationException(exceptionMessage);
            }

            var tenantFilterMiddleware = new TeamsTenantFilteringMiddleware(allowedTenants);
            this.Use(tenantFilterMiddleware);
        }
    }
}