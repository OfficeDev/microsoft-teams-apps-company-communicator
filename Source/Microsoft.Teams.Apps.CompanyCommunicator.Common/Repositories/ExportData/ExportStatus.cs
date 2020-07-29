// <copyright file="ExportStatus.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData
{
    /// <summary>
    /// Export telemetry status.
    /// </summary>
    public enum ExportStatus
    {
        /// <summary>
        /// This represents the export is scheduled.
        /// </summary>
        New,

        /// <summary>
        /// This represents the export is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// This reprsents the export is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// This represents the export is failed.
        /// </summary>
        Failed,
    }
}
