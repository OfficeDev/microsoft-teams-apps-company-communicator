// <copyright file="BotController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;

    /// <summary>
    /// Message controller for the bot.
    /// </summary>
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter adapter;
        private readonly IBot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotController"/> class.
        /// Dependency Injection will provide the Adapter and IBot implementation at runtime.
        /// </summary>
        /// <param name="adapter">Bot framework http adpater instance.</param>
        /// <param name="bot">Bot instance.</param>
        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            this.adapter = adapter;
            this.bot = bot;
        }

        /// <summary>
        /// POST: api/Messages
        /// Delegate the processing of the HTTP POST to the adapter.
        /// The adapter will invoke the bot.
        /// </summary>
        /// <returns>An instance of <see cref="System.Threading.Tasks.Task"/> class.</returns>
        [HttpPost]
        public async Task PostAsync()
        {
            await this.adapter.ProcessAsync(this.Request, this.Response, this.bot);
        }
    }
}
