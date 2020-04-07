// <copyright file="CompanyCommunicatorBotAdapter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// The Company Communicator Bot Adapter.
    /// </summary>
    public class CompanyCommunicatorBotAdapter : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorBotAdapter"/> class.
        /// </summary>
        /// <param name="credentialProvider">Credential provider service instance.</param>
        /// <param name="companyCommunicatorBotFilterMiddleware">Teams message filter middleware instance.</param>
        public CompanyCommunicatorBotAdapter(
            ICredentialProvider credentialProvider,
            CompanyCommunicatorBotFilterMiddleware companyCommunicatorBotFilterMiddleware)
            : base(credentialProvider)
        {
            this.Use(companyCommunicatorBotFilterMiddleware);
        }
    }
}
