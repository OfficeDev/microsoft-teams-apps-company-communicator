// <copyright file="BotController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Teams.Apps.CompanyCommunicator.Bot;

    /// <summary>
    /// Message controller for the bot.
    /// </summary>
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly BotFrameworkHttpAdapter adapter;
        private readonly IBot authorBot;
        private readonly IBot userBot;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotController"/> class.
        /// Dependency Injection will provide the Adapter and IBot implementation at runtime.
        /// </summary>
        /// <param name="adapter">Company Communicator Bot Adapter instance.</param>
        /// <param name="authorBot">Company Communicator Author Bot instance.</param>
        /// <param name="userBot">Company Communicator User Bot instance.</param>
        public BotController(
            CompanyCommunicatorBotAdapter adapter,
            AuthorTeamsActivityHandler authorBot,
            UserTeamsActivityHandler userBot)
        {
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            this.authorBot = authorBot ?? throw new ArgumentNullException(nameof(authorBot));
            this.userBot = userBot ?? throw new ArgumentNullException(nameof(userBot));
        }

        /// <summary>
        /// POST: api/Messages/user
        /// Delegate the processing of the HTTP POST to the adapter.
        /// The adapter will invoke the user bot.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [HttpPost]
        [Route("user")]
        public async Task PostAsync()
        {
            await this.adapter.ProcessAsync(this.Request, this.Response, this.userBot);
        }

        /// <summary>
        /// POST: api/Messages/author
        /// Delegate the processing of the HTTP POST to the adapter.
        /// The adapter will invoke the author bot.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [HttpPost]
        [Route("author")]
        public async Task PostAuthorAsync()
        {
            await this.adapter.ProcessAsync(this.Request, this.Response, this.authorBot);
        }
    }
}
