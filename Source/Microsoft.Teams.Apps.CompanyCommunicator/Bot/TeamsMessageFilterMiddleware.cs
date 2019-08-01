// <copyright file="TeamsMessageFilterMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;

    /// <summary>
    /// The bot's Teams message filter middleware.
    /// </summary>
    public class TeamsMessageFilterMiddleware : IMiddleware
    {
        private static readonly string MsTeamsChannelId = "msteams";

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsMessageFilterMiddleware"/> class.
        /// </summary>
        public TeamsMessageFilterMiddleware()
        {
        }

        /// <summary>
        /// Processes an incoming activity.
        /// If an activity's channel id is not "msteams", then the middleware short circuits the pipeline,
        /// and skips the middlewares and handlers that are listed after this filter in the pipeline.
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of a conversation.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            var isMsTeamsChannel = this.ValidateChannel(turnContext);

            if (isMsTeamsChannel)
            {
                await next(cancellationToken).ConfigureAwait(false);
            }
        }

        private bool ValidateChannel(ITurnContext turnContext)
        {
            return TeamsMessageFilterMiddleware.MsTeamsChannelId.Equals(
                turnContext?.Activity?.ChannelId,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
