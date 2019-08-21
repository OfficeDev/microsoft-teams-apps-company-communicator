// <copyright file="CompanyCommunicatorBotFilterMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The bot's general filter middleware.
    /// </summary>
    public class CompanyCommunicatorBotFilterMiddleware : IMiddleware
    {
        private static readonly string MsTeamsChannelId = "msteams";

        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorBotFilterMiddleware"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        public CompanyCommunicatorBotFilterMiddleware(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Processes an incoming activity.
        /// If the activity's channel id is not "msteams", or its conversation's tenant is not an allowed tenant,
        /// then the middleware short circuits the pipeline, and skips the middlewares and handlers
        /// that are listed after this filter in the pipeline.
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of a conversation.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            var isMsTeamsChannel = this.ValidateChannel(turnContext);
            if (!isMsTeamsChannel)
            {
                return;
            }

            var isAllowedTenant = this.ValidateTenant(turnContext);
            if (!isAllowedTenant)
            {
                return;
            }

            await next(cancellationToken).ConfigureAwait(false);
        }

        private bool ValidateChannel(ITurnContext turnContext)
        {
            var channelId = turnContext?.Activity?.ChannelId;
            if (string.IsNullOrWhiteSpace(channelId))
            {
                var exceptionMessage = "Channel id is missing.";
                Console.WriteLine(exceptionMessage);
                throw new ApplicationException(exceptionMessage);
            }

            return channelId.Equals(
                CompanyCommunicatorBotFilterMiddleware.MsTeamsChannelId,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidateTenant(ITurnContext turnContext)
        {
            var disableTenantFilter = this.configuration.GetValue<bool>("DisableTenantFilter", false);
            if (disableTenantFilter)
            {
                return true;
            }

            var allowedTenantIds = this.configuration
                ?.GetValue<string>("AllowedTenants", string.Empty)
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim());
            if (allowedTenantIds == null || allowedTenantIds.Count() == 0)
            {
                var exceptionMessage = "AllowedTenants setting is not set properly in the configuration file.";
                Console.WriteLine(exceptionMessage);
                throw new ApplicationException(exceptionMessage);
            }

            var tenantId = turnContext?.Activity?.Conversation?.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                var exceptionMessage = "tenant id is missing.";
                Console.WriteLine(exceptionMessage);
                throw new ApplicationException(exceptionMessage);
            }

            return allowedTenantIds.Contains(tenantId);
        }
    }
}
