// <copyright file="GetRecipientDataListForAllUsersActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This Activity represents the "get recipient data list for all users" durable activity.
    /// 1). It takes all users' data entity list as passing in parameter.
    /// 2). It breaks that list of users into batches.
    /// 3). For each batch:
    ///         It initializes the sent notification data table with a row for each recipient.
    ///         It initializes a partitioned batch for that batch in the send batches data.
    /// </summary>
    public class GetRecipientDataListForAllUsersActivity
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly SendBatchesDataRepository sendBatchesDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForAllUsersActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="sendBatchesDataRepository">The send batches data repository.</param>
        public GetRecipientDataListForAllUsersActivity(
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
        /// <param name="allUserDataEntities">All users data entity list.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<RecipientDataListInformation> RunAsync(
            IDurableOrchestrationContext context,
            IEnumerable<UserDataEntity> allUserDataEntities,
            NotificationDataEntity notificationDataEntity)
        {
            var recipientDataListInformation = await context.CallActivityWithRetryAsync<RecipientDataListInformation>(
                nameof(GetRecipientDataListForAllUsersActivity.GetAllUsersRecipientDataListAsync),
                ActivitySettings.CommonActivityRetryOptions,
                (notificationDataEntity.Id, allUserDataEntities));

            return recipientDataListInformation;
        }

        /// <summary>
        /// This method represents the "get recipient data list for all users" durable activity.
        /// 1). It takes all users' data entity list as passing in parameter.
        /// 2). It breaks that list of users into batches.
        /// 3). For each batch:
        ///         It initializes the sent notification data table with a row for each recipient.
        ///         It initializes a partitioned batch for that batch in the send batches data.
        /// </summary>
        /// <param name="dto">An instance of GetAllUserRecipientDataListActivityDTO.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetAllUsersRecipientDataListAsync))]
        public async Task<RecipientDataListInformation> GetAllUsersRecipientDataListAsync(
            [ActivityTrigger]
            (string NotificationDataEntityId, IEnumerable<UserDataEntity> AllUsersDataEntityList) dto)
        {
            var userDataEntities = dto.AllUsersDataEntityList;

            var userDataEntityList = userDataEntities.ToList();
            var userDataEntityBatches = userDataEntityList.SeparateIntoBatches();

            var recipientDataListInformation = new RecipientDataListInformation
            {
                TotalNumberOfRecipients = userDataEntityList.Count,
                NumberOfRecipientDataBatches = userDataEntityBatches.Count,
            };

            var batchIndex = 1;
            foreach (var userDataEntityBatch in userDataEntityBatches)
            {
                // Create a new separate batch to store in each data table.
                var sentNotificationDataRepositoryBatch = new List<SentNotificationDataEntity>();
                var sendBatchesDataRepositoryBatch = new List<SentNotificationDataEntity>();

                // Get the batch partition key to be used for every entity in the current batch.
                var batchPartitionKey = this.sendBatchesDataRepository.GetBatchPartitionKey(
                    notificationId: dto.NotificationDataEntityId,
                    batchIndex: batchIndex);

                // Iterate the users in the batch and create the appropriate entity to
                // store in each data table.
                foreach (var userDataEntity in userDataEntityBatch)
                {
                    // The partition key for the SentNotificationData table is just the notification Id.
                    sentNotificationDataRepositoryBatch.Add(
                        userDataEntity.CreateInitialSentNotificationDataEntity(
                            partitionKey: dto.NotificationDataEntityId));

                    // The SendBatchesData table is separated into batches based on the partition key, so
                    // the batchPartitionKey is used here.
                    sendBatchesDataRepositoryBatch.Add(
                        userDataEntity.CreateInitialSentNotificationDataEntity(
                            partitionKey: batchPartitionKey));
                }

                await Task.WhenAll(
                    this.sentNotificationDataRepository.InsertOrMergeOneBatchAsync(sentNotificationDataRepositoryBatch),
                    this.sendBatchesDataRepository.InsertOrMergeOneBatchAsync(sendBatchesDataRepositoryBatch));

                batchIndex++;
            }

            return recipientDataListInformation;
        }
    }
}
