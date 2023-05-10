// <copyright file="CompanyCommunicatorBotAdapter.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// The Company Communicator Bot Adapter.
    /// </summary>
    public class CompanyCommunicatorBotAdapter : CloudAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorBotAdapter"/> class.
        /// </summary>
        /// <param name="companyCommunicatorBotFilterMiddleware">Teams message filter middleware instance.</param>
        /// <param name="botFrameworkAuthentication">Bot framework authentication.</param>
        public CompanyCommunicatorBotAdapter(
            CompanyCommunicatorBotFilterMiddleware companyCommunicatorBotFilterMiddleware,
            BotFrameworkAuthentication botFrameworkAuthentication)
            : base(botFrameworkAuthentication)
        {
            companyCommunicatorBotFilterMiddleware = companyCommunicatorBotFilterMiddleware ?? throw new ArgumentNullException(nameof(companyCommunicatorBotFilterMiddleware));

            // Middleware
            this.Use(companyCommunicatorBotFilterMiddleware);
        }
    }
}
