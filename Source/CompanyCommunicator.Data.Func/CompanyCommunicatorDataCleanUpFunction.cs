// <copyright file="CompanyCommunicatorDataCleanUpFunction.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.CleanUpHistory;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.DataCleanUpServices;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App HTTP triggered.
    /// Used for deleting the entities for different tables in Azure table storage.
    /// </summary>
    public class CompanyCommunicatorDataCleanUpFunction
    {
        /// <summary>
        /// Maximum size of batch for allowed Table Batch Operation.
        /// </summary>
        public const int MaxBatchSize = 100;
        private readonly IPartitionKeyHandler partitionKeyHandler;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ICleanUpHistoryRepository cleanUpHistoryRepository;
        private readonly TableRowKeyGenerator tableRowKeyGenerator;
        private string tableRowKey = string.Empty;
        private int totalDeletedRecords = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorDataCleanUpFunction"/> class.
        /// </summary>
        /// <param name="partitionKeyHandler">Partition logic handler.</param>
        /// <param name="sentNotificationDataRepository">The SentNotificationData repository.</param>
        /// <param name="notificationDataRepository">The NotificationData repository.</param>
        /// <param name="cleanUpHistoryRepository">The cleanUpHistory repository.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public CompanyCommunicatorDataCleanUpFunction(
            IPartitionKeyHandler partitionKeyHandler,
            ISentNotificationDataRepository sentNotificationDataRepository,
            INotificationDataRepository notificationDataRepository,
            ICleanUpHistoryRepository cleanUpHistoryRepository,
            TableRowKeyGenerator tableRowKeyGenerator)
        {
            this.partitionKeyHandler = partitionKeyHandler ?? throw new ArgumentNullException(nameof(partitionKeyHandler));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.cleanUpHistoryRepository = cleanUpHistoryRepository ?? throw new ArgumentNullException(nameof(cleanUpHistoryRepository));
            this.tableRowKeyGenerator = tableRowKeyGenerator ?? throw new ArgumentNullException(nameof(tableRowKeyGenerator));
        }

        /// <summary>
        /// Azure Function App HTTP triggered.
        /// Used for triggering the clean data from Azure Table storage.
        /// </summary>
        /// <param name="request">The Request message.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorDataCleanUpFunction")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequestMessage request, ILogger log)
        {
            var content = request.Content;
            if (string.IsNullOrEmpty(content.ReadAsStringAsync().Result))
            {
                return request.CreateResponse(HttpStatusCode.NoContent, "No Content received");
            }

            string jsonContent = content.ReadAsStringAsync().Result;
            dynamic requestPram = JsonConvert.DeserializeObject<Models.DeleteMessage>(jsonContent);

            try
            {
                // Validate the required param
                if (string.IsNullOrEmpty(requestPram.RowKeyId))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Error in getting the Row key Id");
                }

                if (string.IsNullOrEmpty(requestPram.SelectedDateRange))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Error in getting the Selected Date Range");
                }

                if (string.IsNullOrEmpty(requestPram.DeletedBy))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Error in getting the Deleted By");
                }

                if (string.IsNullOrEmpty(requestPram.StartDate))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Error in getting the Start Date");
                }

                if (string.IsNullOrEmpty(requestPram.EndDate))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Error in getting the End Date");
                }

                await Task.WhenAll(
                   this.sentNotificationDataRepository.EnsureSentNotificationDataTableExistsAsync(),
                   this.cleanUpHistoryRepository.EnsureCleanUpHistoryTableExistsAsync());
                this.tableRowKey = requestPram.RowKeyId;
                await this.cleanUpHistoryRepository.CreateOrUpdateAsync(new CleanUpHistoryEntity()
                {
                    PartitionKey = "Delete Messages",
                    RowKey = requestPram.RowKeyId,
                    SelectedDateRange = requestPram.SelectedDateRange,
                    RecordsDeleted = 0,
                    DeletedBy = requestPram.DeletedBy,
                    Status = CleanUpStatus.InProgress.ToString(),
                    StartDate = requestPram.StartDate,
                    EndDate = requestPram.EndDate,
                });

                var inputStartDate = requestPram.StartDate;
                var inputEndDate = requestPram.EndDate;
                var fromDate = DateTimeOffset.ParseExact(inputStartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                var toDate = DateTimeOffset.ParseExact(inputEndDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                if (requestPram.SelectedDateRange != "customDate")
                {
                    toDate = toDate.AddDays(1);
                }
                await this.PurgeEntitiesAsync(fromDate, toDate, log, this.sentNotificationDataRepository.Table).ConfigureAwait(false);

                await this.cleanUpHistoryRepository.CreateOrUpdateAsync(new CleanUpHistoryEntity()
                {
                    PartitionKey = "Delete Messages",
                    RowKey = requestPram.RowKeyId,
                    SelectedDateRange = requestPram.SelectedDateRange,
                    RecordsDeleted = this.totalDeletedRecords,
                    DeletedBy = requestPram.DeletedBy,
                    Status = CleanUpStatus.InProgress.ToString(),
                    StartDate = requestPram.StartDate,
                    EndDate = requestPram.EndDate,
                });

                await this.PurgeEntitiesAsync(fromDate, toDate, log, this.notificationDataRepository.Table).ConfigureAwait(false);
                await this.cleanUpHistoryRepository.CreateOrUpdateAsync(new CleanUpHistoryEntity()
                {
                    PartitionKey = "Delete Messages",
                    RowKey = requestPram.RowKeyId,
                    SelectedDateRange = requestPram.SelectedDateRange,
                    RecordsDeleted = this.totalDeletedRecords,
                    DeletedBy = requestPram.DeletedBy,
                    Status = CleanUpStatus.Completed.ToString(),
                    StartDate = requestPram.StartDate,
                    EndDate = requestPram.EndDate,
                });

                return request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred while purging data: {ex.Message}.");
                await this.cleanUpHistoryRepository.CreateOrUpdateAsync(new CleanUpHistoryEntity()
                {
                    PartitionKey = "Delete Messages",
                    RowKey = requestPram.RowKeyId,
                    SelectedDateRange = requestPram.SelectedDateRange,
                    RecordsDeleted = this.totalDeletedRecords,
                    DeletedBy = requestPram.DeletedBy,
                    Status = CleanUpStatus.Failed.ToString(),
                    StartDate = requestPram.StartDate,
                    EndDate = requestPram.EndDate,
                });
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// To purge the entities for different Azure tables based between the provided start and end time.
        /// </summary>
        /// <param name="purgeStartDate">Purge Start Date.</param>
        /// <param name="purgeEndDate">Purge End Date.</param>
        /// <param name="log">the Logger.</param>
        /// <param name="table">The table name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<Tuple<int, int>> PurgeEntitiesAsync(DateTimeOffset purgeStartDate, DateTimeOffset purgeEndDate, ILogger log, CloudTable table)
        {
            var sw = new Stopwatch();
            sw.Start();

            log.LogInformation($"Starting PurgeEntitiesAsync");
            TimeSpan startDate = DateTimeOffset.UtcNow.Date - purgeStartDate;
            TimeSpan endDate = DateTimeOffset.UtcNow.Date - purgeEndDate;
            var purgeRecordsOlderThanDaysStartDate = startDate.Days;
            var purgeRecordsOlderThanDaysEndDate = endDate.Days;
            var query = new TableQuery();

            log.LogInformation($"Starting PurgeEntitiesAsync");
            log.LogInformation($"Table={table.Name}, PurgeRecordsStartDate={purgeRecordsOlderThanDaysStartDate} PurgeRecordsEndDate={purgeRecordsOlderThanDaysEndDate}");

            if (table.Name == "NotificationData")
            {
                query = this.partitionKeyHandler.GetNotificationDataTableQuery(purgeRecordsOlderThanDaysStartDate, purgeRecordsOlderThanDaysEndDate);
            }
            else
            {
                query = this.partitionKeyHandler.GetTableQuery(purgeRecordsOlderThanDaysStartDate, purgeRecordsOlderThanDaysEndDate);
            }

            var continuationToken = new TableContinuationToken();
            int numPagesProcessed = 0;
            int numEntitiesDeleted = 0;

            do
            {
                var page = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                var pageNumber = numPagesProcessed + 1;

                if (page.Results.Count == 0)
                {
                    if (numPagesProcessed == 0)
                    {
                        log.LogDebug($"No entities were available for purging");
                    }

                    break;
                }

                var partitionsFromPage = this.partitionKeyHandler.GetPartitionsFromPage(page.Results);

                log.LogDebug($"Page {pageNumber}: number of partitions grouped by PartitionKey: {partitionsFromPage.Count}");

                var tasks = new List<Task<int>>();

                foreach (var partition in partitionsFromPage)
                {
                    var chunkedPartition = partition.Chunk(MaxBatchSize).ToList();

                    foreach (var batch in chunkedPartition)
                    {
                        // All deletes asynchronously
                        tasks.Add(this.DeleteRecordsAsync(table, batch.ToList(), log));
                    }
                }

                // Wait for and consolidate results
                await Task.WhenAll(tasks);
                var numEntitiesDeletedInThisPage = tasks.Sum(t => t.GetAwaiter().GetResult());
                numEntitiesDeleted += numEntitiesDeletedInThisPage;
                log.LogDebug($"Page {pageNumber}: processing complete, {numEntitiesDeletedInThisPage} entities deleted");

                continuationToken = page.ContinuationToken;
                numPagesProcessed++;
            }
            while (continuationToken != null);

            var entitiesPerSecond = numEntitiesDeleted > 0 ? (int)(numEntitiesDeleted / sw.Elapsed.TotalSeconds) : 0;
            var msPerEntity = numEntitiesDeleted > 0 ? (int)(sw.Elapsed.TotalMilliseconds / numEntitiesDeleted) : 0;
            this.totalDeletedRecords = this.totalDeletedRecords + numEntitiesDeleted;
            log.LogInformation($"Finished PurgeEntitiesAsync, processed {numPagesProcessed} pages and deleted {numEntitiesDeleted} entities in {sw.Elapsed} ({entitiesPerSecond} entities per second, or {msPerEntity} ms per entity)");

            return new Tuple<int, int>(numPagesProcessed, numEntitiesDeleted);
        }

        /// <summary>
        /// Executes a batch delete operation on different table records.
        /// </summary>
        /// <param name="table">Cloud Table reference.</param>
        /// <param name="batch">List of Batches of records.</param>
        /// <param name="log">The logger.</param>
        /// <returns>batch.Count <see cref="int"/> represents the count of batch items executed for deletion.</returns>
        private async Task<int> DeleteRecordsAsync(Azure.Cosmos.Table.CloudTable table, IList<DynamicTableEntity> batch, ILogger log)
        {
            if (batch.Count > MaxBatchSize)
            {
                throw new ArgumentException($"Batch size of {batch.Count} is larger than the maximum allowed size of {MaxBatchSize}");
            }

            var partitionKey = batch.First().PartitionKey;

            if (batch.Any(entity => entity.PartitionKey != partitionKey))
            {
                throw new ArgumentException($"Not all entities in the batch contain the same partitionKey - {partitionKey}");
            }

            log.LogTrace($"Deleting {batch.Count} rows from partitionKey={partitionKey}");

            var batchOperation = new TableBatchOperation();

            foreach (var entity in batch)
            {
                batchOperation.Delete(entity);
            }

            try
            {
                await this.cleanUpHistoryRepository.EnsureCleanUpHistoryTableExistsAsync().ConfigureAwait(false);
                await table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
                return batch.Count;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404 &&
                    ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "ResourceNotFound")
                {
                    log.LogWarning($"Failed to delete rows from partitionKey={partitionKey}. Data has already been deleted, ex.Message={ex.Message}, HttpStatusCode={ex.RequestInformation.HttpStatusCode}, ErrorCode={ex.RequestInformation.ExtendedErrorInformation.ErrorCode}, ErrorMessage={ex.RequestInformation.ExtendedErrorInformation.ErrorMessage}");
                    return 0;
                }

                log.LogError($"Failed to delete rows from partitionKey={partitionKey}. Unknown error. ex.Message={ex.Message}, HttpStatusCode={ex.RequestInformation.HttpStatusCode}, ErrorCode={ex.RequestInformation.ExtendedErrorInformation.ErrorCode}, ErrorMessage={ex.RequestInformation.ExtendedErrorInformation.ErrorMessage}");
                throw;
            }
        }
    }
}
