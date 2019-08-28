// <copyright file="GetBotConversationMemebersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// Draft notification preview service.
    /// </summary>
    public class GetBotConversationMemebersService : ContinueBotConversationService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetBotConversationMemebersService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration service.</param>
        /// <param name="companyCommunicatorBotAdapter">Bot framework http adapter instance.</param>
        public GetBotConversationMemebersService(
            IConfiguration configuration,
            CompanyCommunicatorBotAdapter companyCommunicatorBotAdapter)
            : base(configuration, companyCommunicatorBotAdapter)
        {
        }

        /// <summary>
        /// Send a preview of a draft notification.
        /// </summary>
        /// <param name="teamDataEntity">The team data entity.</param>
        /// <returns>It returns HttpStatusCode.OK, if this method triggers the bot service to send the adaptive card successfully.
        /// It returns HttpStatusCode.TooManyRequests, if the bot service throttled the request to send the adaptive card.</returns>
        public async Task<IEnumerable<ChannelAccount>> GetBotConversationMembersAsync(TeamDataEntity teamDataEntity)
        {
            IList<ChannelAccount> members = null;

            async Task BotCallbackHandler(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                members = await this.CompanyCommunicatorBotAdapter.GetConversationMembersAsync(turnContext, CancellationToken.None);
            }

            // Todo: handle too many requests exception.
            var httpStatusCode = await this.ContinueBotConversationAsync(teamDataEntity, BotCallbackHandler);

            return members ?? new List<ChannelAccount>();
        }
    }
}