// <copyright file="TeamsConversationOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func
{
    /// <summary>
    /// Options for Teams Conversation.
    /// </summary>
    public class TeamsConversationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether user app should be proactively installed.
        /// </summary>
        public bool ProactivelyInstallUserApp { get; set; }

        /// <summary>
        /// Gets or sets maximum attempts to create conversation with teams user.
        /// </summary>
        public int MaxAttemptsToCreateConversation { get; set; }
    }
}
