// <copyright file="PartitionKeyHandlerService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.DataCleanUpServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Partition Key Handler code for the Azure Table Storage.
    /// </summary>
    public class PartitionKeyHandlerService : IPartitionKeyHandler
    {
        private readonly ILogger<PartitionKeyHandlerService> log;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionKeyHandlerService"/> class.
        /// </summary>
        /// <param name="log">The logging service.</param>
        public PartitionKeyHandlerService(ILogger<PartitionKeyHandlerService> log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Used to create the table query based on start and end date for records deletion.
        /// </summary>
        /// <param name="purgeRecordsOlderThanDaysStartDate">Start Date for records deletion.</param>
        /// <param name="purgeRecordsOlderThanDaysEndDate">End date for records deletion.</param>
        /// <returns>Table Query based on the start and end date.</returns>
        public TableQuery GetTableQuery(int purgeRecordsOlderThanDaysStartDate, int purgeRecordsOlderThanDaysEndDate)
        {
            var fromDateFilter = TableQuery.GenerateFilterConditionForDate(
               nameof(TableEntity.Timestamp),
               QueryComparisons.GreaterThanOrEqual,
               DateTimeOffset.UtcNow.Date.AddDays(-purgeRecordsOlderThanDaysStartDate));
            var toDateFilter = TableQuery.GenerateFilterConditionForDate(
               nameof(TableEntity.Timestamp),
               QueryComparisons.LessThanOrEqual,
               DateTimeOffset.UtcNow.Date.AddDays(1).AddDays(-purgeRecordsOlderThanDaysEndDate));

            var completeFilter = TableQuery.CombineFilters(toDateFilter, TableOperators.And, fromDateFilter);


            var query = new TableQuery()
                .Where(completeFilter)
                .Select(new[] { "PartitionKey", "RowKey" });
            return query;
        }

        /// <summary>
        /// Used to create the table query based on start, end date and partition key for records deletion.
        /// </summary>
        /// <param name="purgeRecordsOlderThanDaysStartDate">Start Date for records deletion.</param>
        /// <param name="purgeRecordsOlderThanDaysEndDate">End date for records deletion.</param>
        /// <returns>Table Query based on the start and end date.</returns>
        public TableQuery GetNotificationDataTableQuery(int purgeRecordsOlderThanDaysStartDate, int purgeRecordsOlderThanDaysEndDate)
        {
            var fromDateFilter = TableQuery.GenerateFilterConditionForDate(
               nameof(TableEntity.Timestamp),
               QueryComparisons.GreaterThanOrEqual,
               DateTimeOffset.UtcNow.Date.AddDays(-purgeRecordsOlderThanDaysStartDate));
            var toDateFilter = TableQuery.GenerateFilterConditionForDate(
               nameof(TableEntity.Timestamp),
               QueryComparisons.LessThanOrEqual,
               DateTimeOffset.UtcNow.Date.AddDays(1).AddDays(-purgeRecordsOlderThanDaysEndDate));
            var partitionKeyFilter = TableQuery.GenerateFilterCondition(
               nameof(TableEntity.PartitionKey),
               QueryComparisons.NotEqual,
               "DraftNotifications");

            var completeFilter = TableQuery.CombineFilters(TableQuery.CombineFilters(toDateFilter, TableOperators.And, fromDateFilter), TableOperators.And, partitionKeyFilter);

            var query = new TableQuery()
                .Where(completeFilter)
                .Select(new[] { "PartitionKey", "RowKey" });
            return query;
        }

        /// <summary>
        /// Breaks up a result page into partitions grouped by PartitionKey.
        /// </summary>
        /// <param name="page">List of records in a page.</param>
        /// <returns>Lists which represents the partitioned list of page grouped by partition key.</returns>
        public IList<IList<DynamicTableEntity>> GetPartitionsFromPage(IList<DynamicTableEntity> page)
        {
            var result = new List<IList<DynamicTableEntity>>();

            var groupByResult = page.GroupBy(x => x.PartitionKey);

            foreach (var partition in groupByResult.ToList())
            {
                var partitionAsList = partition.ToList();
                result.Add(partitionAsList);
            }

            return result;
        }
    }
}
