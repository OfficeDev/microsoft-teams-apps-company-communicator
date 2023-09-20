// <copyright file="CleanUpStatus.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.CleanUpHistory
{
    /// <summary>
    /// Cleanup telemetry status.
    /// </summary>
    public enum CleanUpStatus
    {
        /// <summary>
        /// This represents the cleanup is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// This represents the cleanup is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// This represents the cleanup is failed.
        /// </summary>
        Failed,
    }
}