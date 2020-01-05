// <copyright file="CreateConversationResultType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationService
{
    /// <summary>
    /// An enum indicating the different create conversation result types.
    /// </summary>
    public enum CreateConversationResultType
    {
        /// <summary>
        /// Type indicating creating the conversation succeeded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Type indicating creating the conversation was throttled.
        /// </summary>
        Throttled,

        /// <summary>
        /// Type indicating creating the conversation failed.
        /// </summary>
        Failed,
    }
}
