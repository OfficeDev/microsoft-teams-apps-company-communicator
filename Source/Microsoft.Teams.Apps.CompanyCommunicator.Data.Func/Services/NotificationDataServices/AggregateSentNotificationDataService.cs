// <copyright file="AggregateSentNotificationDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// A service that fetches and aggregates the Sent Notification Data results.
    /// </summary>
    public class AggregateSentNotificationDataService
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateSentNotificationDataService"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public AggregateSentNotificationDataService(SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Fetches all of the current known results for the Sent Notification and calculates the various totals
        /// as results.
        /// </summary>
        /// <param name="notificationId">The notification ID.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<AggregatedSentNotificationDataResults> AggregateSentNotificationDataResultsAsync(
            string notificationId)
        {
            var partitionKeyFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                notificationId);

            // The query is based on the delivery status types that are currently aggregated.
            var succeededDeliveryStatusFilter = TableQuery.GenerateFilterCondition(
                nameof(SentNotificationDataEntity.DeliveryStatus),
                QueryComparisons.Equal,
                SentNotificationDataEntity.Succeeded);

            var failedDeliveryStatusFilter = TableQuery.GenerateFilterCondition(
                nameof(SentNotificationDataEntity.DeliveryStatus),
                QueryComparisons.Equal,
                SentNotificationDataEntity.Failed);

            var throttledDeliveryStatusFilter = TableQuery.GenerateFilterCondition(
                nameof(SentNotificationDataEntity.DeliveryStatus),
                QueryComparisons.Equal,
                SentNotificationDataEntity.Throttled);

            // Create the complete query where:
            // PartitionKey eq notificationId AND
            //      DeliveryStatus eq Succeeded OR
            //      DeliveryStatus eq Failed OR
            //      DeliveryStatus eq Throttled
            var partialDeliveryStatusFilter = TableQuery.CombineFilters(succeededDeliveryStatusFilter, TableOperators.Or, failedDeliveryStatusFilter);
            var completeDeliveryStatusFilter = TableQuery.CombineFilters(partialDeliveryStatusFilter, TableOperators.Or, throttledDeliveryStatusFilter);
            var completeFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, completeDeliveryStatusFilter);
            var query = new TableQuery<SentNotificationDataEntity>().Where(completeFilter);

            try
            {
                var aggregatedResults = new AggregatedSentNotificationDataResults();

                TableContinuationToken currentContinuationToken = null;

                do
                {
                    // Make the query to the data table and update the continuation token in order to continue to paginate the results.
                    TableQuerySegment<SentNotificationDataEntity> resultSegment = await this.sentNotificationDataRepository.Table
                        .ExecuteQuerySegmentedAsync<SentNotificationDataEntity>(query, currentContinuationToken);
                    currentContinuationToken = resultSegment.ContinuationToken;

                    // Aggregate the results.
                    foreach (var sentNotification in resultSegment)
                    {
                        aggregatedResults.UpdateAggregatedResults(sentNotification);
                    }
                }
                while (currentContinuationToken != null);

                return aggregatedResults;
            }
            catch
            {
                throw;
            }
        }
    }
}
