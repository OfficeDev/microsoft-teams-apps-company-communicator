// <copyright file="ExportDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Export notification entity class.
    /// This entity holds all of the information about export.
    /// </summary>
    public class ExportDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the file name for the export data.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the response id of the File Consent Card.
        /// </summary>
        public string FileConsentId { get; set; }

        /// <summary>
        /// Gets or sets the DateTime of exporting the notification.
        /// </summary>
        public DateTime? SentDate { get; set; }

        /// <summary>
        /// Gets or sets the status of the export.
        /// </summary>
        public string Status { get; set; }
    }
}
