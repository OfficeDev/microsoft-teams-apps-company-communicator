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
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The bot's general filter middleware.
    /// </summary>
    public class CompanyCommunicatorBotFilterMiddleware : IMiddleware
    {
        private static readonly string MsTeamsChannelId = "msteams";
        private readonly bool disableTenantFilter;
        private readonly string allowedTenants;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorBotFilterMiddleware"/> class.
        /// </summary>
        /// <param name="botFilterMiddlewareOptions">The bot filter middleware options.</param>
        public CompanyCommunicatorBotFilterMiddleware(IOptions<BotFilterMiddlewareOptions> botFilterMiddlewareOptions)
        {
            this.disableTenantFilter = botFilterMiddlewareOptions.Value.DisableTenantFilter;
            this.allowedTenants = botFilterMiddlewareOptions.Value.AllowedTenants;
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
            var isMsTeamsChannel = this.ValidateBotFrameworkChannelId(turnContext);
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

        private bool ValidateBotFrameworkChannelId(ITurnContext turnContext)
        {
            return CompanyCommunicatorBotFilterMiddleware.MsTeamsChannelId.Equals(
                turnContext?.Activity?.ChannelId,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidateTenant(ITurnContext turnContext)
        {
            if (this.disableTenantFilter)
            {
                return true;
            }

            var allowedTenantIds = this.allowedTenants
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim());
            if (allowedTenantIds == null || !allowedTenantIds.Any())
            {
                var exceptionMessage = "AllowedTenants setting is not set properly in the configuration file.";
                Console.WriteLine(exceptionMessage);
                throw new ApplicationException(exceptionMessage);
            }

            var tenantId = turnContext?.Activity?.Conversation?.TenantId;
            return allowedTenantIds.Contains(tenantId);
        }
    }
}
