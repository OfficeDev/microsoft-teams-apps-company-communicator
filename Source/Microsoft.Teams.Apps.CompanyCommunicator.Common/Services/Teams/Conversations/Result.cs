// <copyright file="Result.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams
{
    /// <summary>
    /// An enum indicating the different create user conversation result types.
    /// </summary>
    public enum Result
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
