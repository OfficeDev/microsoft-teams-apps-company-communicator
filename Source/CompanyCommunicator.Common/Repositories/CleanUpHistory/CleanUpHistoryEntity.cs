// <copyright file="CleanUpHistoryEntity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.CleanUpHistory
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Clean Up History entity class.
    /// This entity holds all of the information about Deletion of records.
    /// </summary>
    public class CleanUpHistoryEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the selected date range for the delete data.
        /// </summary>
        public string SelectedDateRange { get; set; }

        /// <summary>
        /// Gets or sets the number of records deleted.
        /// </summary>
        public int RecordsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the Deleted By field.
        /// </summary>
        public string DeletedBy { get; set; }

        /// <summary>
        /// Gets or sets the status of the deletion.
        /// </summary>
        public string Status { get; set; }

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
