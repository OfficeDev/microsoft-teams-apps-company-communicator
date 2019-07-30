// <copyright file="TeamFilterMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;

    /// <summary>
    /// Middleware for translating text between the user and bot.
    /// Uses the Microsoft Translator Text API.
    /// </summary>
    public class TeamFilterMiddleware : IMiddleware
    {
        private static readonly string MsTeamsChannelId = "msteams";

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamFilterMiddleware"/> class.
        /// </summary>
        public TeamFilterMiddleware()
        {
        }

        /// <summary>
        /// Processes an incoming activity.
        /// If an activity meets the following condition(s), the bot will not be executed. It return 200 Ok to client directly.
        /// * If channel is not "msteams".
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
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
            return turnContext?.Activity?.ChannelId == MsTeamsChannelId;
        }
    }
}
