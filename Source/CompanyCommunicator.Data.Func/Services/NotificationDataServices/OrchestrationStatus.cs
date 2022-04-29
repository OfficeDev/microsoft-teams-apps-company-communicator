// <copyright file="OrchestrationStatus.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    /// <summary>
    /// Orchestration instance status.
    /// </summary>
    public enum OrchestrationStatus
    {
        /// <summary>
        /// Orchestration instance is Running.
        /// </summary>
        Running,

        /// <summary>
        /// Orchestration instance is Terminated.
        /// </summary>
        Terminated,

        /// <summary>
        /// Orchestration instance is Completed.
        /// </summary>
        Completed,
    }
}
