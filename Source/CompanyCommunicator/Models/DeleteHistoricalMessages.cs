// <copyright file="DeleteHistoricalMessages.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Delete message model class.
    /// </summary>
    public class DeleteHistoricalMessages
    {
        /// <summary>
        /// Gets or sets the table row key id.
        /// </summary>
        public string RowKeyId { get; set; }

        /// <summary>
        /// Gets or sets the selected date range for the delete data.
        /// </summary>
        public string SelectedDateRange { get; set; }

        /// <summary>
        /// Gets or sets the Deleted By field.
        /// </summary>
        public string DeletedBy { get; set; }

        /// <summary>
        /// Gets or sets the Start Date field.
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the End Date field.
        /// </summary>
        public string EndDate { get; set; }
    }
}
