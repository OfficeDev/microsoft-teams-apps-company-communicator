// <copyright file="AdapterWithTeamFilter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// Bot adapter with teams filter.
    /// </summary>
    public class AdapterWithTeamFilter : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterWithTeamFilter"/> class.
        /// </summary>
        /// <param name="credentialProvider">Credential provider serive instance.</param>
        /// <param name="teamFilterMiddleware">Team filter middleware instance.</param>
        public AdapterWithTeamFilter(
            ICredentialProvider credentialProvider,
            TeamFilterMiddleware teamFilterMiddleware)
            : base(credentialProvider)
        {
            this.Use(teamFilterMiddleware);
        }
    }
}