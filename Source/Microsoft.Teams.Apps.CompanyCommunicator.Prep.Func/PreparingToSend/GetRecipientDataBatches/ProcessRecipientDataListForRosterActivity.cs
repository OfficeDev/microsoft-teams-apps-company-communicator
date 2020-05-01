// <copyright file="ProcessRecipientDataListForRosterActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This Activity processes the entire sent notification data table that
    /// has been initialized by fetching all of the team rosters and stores
    /// the entities as partitioned batches in the send batches data table.
    /// 1). Fetch the sent notification data.
    /// 2). Separate the data into batches.
    /// 2). Create the correct batch partition key for each batch and set
    ///     the partition keys for each batch.
    /// 3). Store these partitioned batches in the send batches data table
    ///     one batch at a time.
    /// </summary>
    public class ProcessRecipientDataListForRosterActivity
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly SendBatchesDataRepository sendBatchesDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessRecipientDataListForRosterActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        /// <param name="sendBatchesDataRepository">The send batches data repository.</param>
        public ProcessRecipientDataListForRosterActivity(
            SentNotificationDataRepository sentNotificationDataRepository,
            SendBatchesDataRepository sendBatchesDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.sendBatchesDataRepository = sendBatchesDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<RecipientDataListInformation> RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId)
        {
            var recipientDataListInformation =
                await context.CallActivityWithRetryAsync<RecipientDataListInformation>(
                    nameof(ProcessRecipientDataListForRosterActivity.ProcessRecipientDataListForRosterAsync),
                    ActivitySettings.CommonActivityRetryOptions,
                    notificationDataEntityId);

            return recipientDataListInformation;
        }

        /// <summary>
        /// This method processes the entire sent notification data table that
        /// has been initialized by fetching all of the team rosters and stores
        /// the entities as partitioned batches in the send batches data table.
        /// 1). Fetch the sent notification data.
        /// 2). Separate the data into batches.
        /// 2). Create the correct batch partition key for each batch and set
        ///     the partition keys for each batch.
        /// 3). Store these partitioned batches in the send batches data table
        ///     one batch at a time.
        /// </summary>
        /// <param name="notificationDataEntityId">The notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(ProcessRecipientDataListForRosterAsync))]
        public async Task<RecipientDataListInformation> ProcessRecipientDataListForRosterAsync(
            [ActivityTrigger] string notificationDataEntityId)
        {
            var sentNotificationDataEntities =
                await this.sentNotificationDataRepository.GetAllAsync(notificationDataEntityId);

            var sentNotificationDataEntityList = sentNotificationDataEntities.ToList();
            var sentNotificationDataEntityBatches = sentNotificationDataEntityList.SeparateIntoBatches();

            var recipientDataListInformation = new RecipientDataListInformation
            {
                TotalNumberOfRecipients = sentNotificationDataEntityList.Count,
                NumberOfRecipientDataBatches = sentNotificationDataEntityBatches.Count,
            };

            var batchIndex = 1;
            foreach (var sentNotificationDataEntityBatch in sentNotificationDataEntityBatches)
            {
                // Get the batch partition key to be used for every entity in the current batch.
                var batchPartitionKey = this.sendBatchesDataRepository.GetBatchPartitionKey(
                    notificationId: notificationDataEntityId,
                    batchIndex: batchIndex);

                // The SendBatchesData table is separated into batches based on the parition key, so
                // set all of the partition keys for the entities in this batch to the batchPartitionKey.
                var sendBatchesDataRepositoryBatch = sentNotificationDataEntityBatch.Select(e =>
                    {
                        e.PartitionKey = batchPartitionKey;
                        return e;
                    })
                    .ToList();

                await this.sendBatchesDataRepository.InsertOrMergeOneBatchAsync(sendBatchesDataRepositoryBatch);

                batchIndex++;
            }

            return recipientDataListInformation;
        }
    }
}
