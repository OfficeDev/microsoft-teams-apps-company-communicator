// <copyright file="MetaData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model
{
    using System;
    using CsvHelper.Configuration.Attributes;

    /// <summary>
    /// Metadata model class.
    /// </summary>
    public class MetaData
    {
        /// <summary>
        /// Gets or sets the message title.
        /// </summary>
        [Name("Message Title")]
        public string MessageTitle { get; set; }

        /// <summary>
        /// Gets or sets the sent timestamp.
        /// </summary>
        [Name("Sent Timestamp")]
        public DateTime? SentTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the export timestamp.
        /// </summary>
        [Name("Export Timestamp")]
        public DateTime? ExportTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the exported by user id.
        /// </summary>
        [Name("ExportedBy")]
        public string ExportedBy { get; set; }
    }
}