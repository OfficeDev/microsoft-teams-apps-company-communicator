// <copyright file="Metadata.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model
{
    using System;

    /// <summary>
    /// Metadata model class.
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// Gets or sets the message title.
        /// </summary>
        public string MessageTitle { get; set; }

        /// <summary>
        /// Gets or sets the sent timestamp.
        /// </summary>
        public DateTime? SentTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the export timestamp.
        /// </summary>
        public DateTime? ExportTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the exported by user id.
        /// </summary>
        public string ExportedBy { get; set; }
    }
}