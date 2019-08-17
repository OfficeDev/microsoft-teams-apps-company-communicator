// <copyright file="CompanyCommunicatorBotAdapter.cs" company="Microsoft">
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
    /// The Company Communicator Bot Adapter.
    /// </summary>
    public class CompanyCommunicatorBotAdapter : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorBotAdapter"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        /// <param name="credentialProvider">Credential provider service instance.</param>
        /// <param name="teamsMessageFilterMiddleware">Teams message filter middleware instance.</param>
        public CompanyCommunicatorBotAdapter(
            IConfiguration configuration,
            ICredentialProvider credentialProvider,
            TeamsMessageFilterMiddleware teamsMessageFilterMiddleware)
            : base(credentialProvider)
        {
            this.Use(teamsMessageFilterMiddleware);

            this.UseTenantFilterMiddleware(configuration);
        }

        private void UseTenantFilterMiddleware(IConfiguration configuration)
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
                var exceptionMessage = "AllowedTenants setting is not set properly in the configuration file.";
                Console.WriteLine(exceptionMessage);
                throw new ApplicationException(exceptionMessage);
            }

            var tenantFilterMiddleware = new TeamsTenantFilteringMiddleware(allowedTenants);
            this.Use(tenantFilterMiddleware);
        }
    }
}