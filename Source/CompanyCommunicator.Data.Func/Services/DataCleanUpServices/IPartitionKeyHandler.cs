// <copyright file="IPartitionKeyHandler.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.DataCleanUpServices
{
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Interface for Partition Key Handler.
    /// </summary>
    public interface IPartitionKeyHandler
    {
        /// <summary>
        /// Used to create the table query based on start and end date for records deletion.
        /// </summary>
        /// <param name="purgeRecordsOlderThanDaysStartDate">Start Date for records deletion.</param>
        /// <param name="purgeRecordsOlderThanDaysEndDate">End date for records deletion.</param>
        /// <returns>Table Query based on the start and end date.</returns>
        TableQuery GetTableQuery(int purgeRecordsOlderThanDaysStartDate, int purgeRecordsOlderThanDaysEndDate);


        /// <summary>
        /// Used to create the table query based on start, end date and partition key for records deletion.
        /// </summary>
        /// <param name="purgeRecordsOlderThanDaysStartDate">Start Date for records deletion.</param>
        /// <param name="purgeRecordsOlderThanDaysEndDate">End date for records deletion.</param>
        /// <returns>Table Query based on the start and end date.</returns>
        TableQuery GetNotificationDataTableQuery(int purgeRecordsOlderThanDaysStartDate, int purgeRecordsOlderThanDaysEndDate);

        /// <summary>
        /// Breaks up a result page into partitions grouped by PartitionKey.
        /// </summary>
        /// <param name="page">List of records in a page.</param>
        /// <returns>Lists which represents the partitioned list of page grouped by partition key.</returns>
        IList<IList<DynamicTableEntity>> GetPartitionsFromPage(IList<DynamicTableEntity> page);
    }
}
