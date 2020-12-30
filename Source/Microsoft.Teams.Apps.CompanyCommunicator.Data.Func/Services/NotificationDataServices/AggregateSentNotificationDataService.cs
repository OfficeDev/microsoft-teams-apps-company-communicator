// <copyright file="AggregateSentNotificationDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// A service that fetches and aggregates the Sent Notification Data results.
    /// </summary>
    public class AggregateSentNotificationDataService
    {
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateSentNotificationDataService"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public AggregateSentNotificationDataService(ISentNotificationDataRepository sentNotificationDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Fetches all of the current known results for the Sent Notification and calculates the various totals
        /// as results.
        /// </summary>
        /// <param name="notificationId">The notification ID.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<AggregatedSentNotificationDataResults> AggregateSentNotificationDataResultsAsync(
            string notificationId,
            ILogger log)
        {
            var partitionKeyFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                notificationId);

            // The SentNotificationDataEntity.DeliveryStatus property's default value is null.
            // After finished processing a recipient, the send function sets the property to one of the following values, which indicates the delivery status.
            //   Succeeded,
            //   Failed,
            //   RecipientNotFound,
            //   Throttled,
            //   etc.
            // The notNullStatusFilter finds out the delivery statuses for all the processed recipients.
            var notNullStatusFilter = TableQuery.GenerateFilterCondition(
                nameof(SentNotificationDataEntity.DeliveryStatus),
                QueryComparisons.NotEqual,
                "null");

            // Create the complete query where:
            // PartitionKey eq notificationId AND DeliveryStatus ne null
            var completeFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, notNullStatusFilter);
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
            catch (Exception e)
            {
                var errorMessage = $"{e.GetType()}: {e.Message}";
                log.LogError(e, $"ERROR: {errorMessage}");
                throw;
            }
        }
    }
}
