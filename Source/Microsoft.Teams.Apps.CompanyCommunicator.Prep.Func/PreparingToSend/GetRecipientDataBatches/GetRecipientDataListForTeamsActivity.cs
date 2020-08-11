// <copyright file="GetRecipientDataListForTeamsActivity.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This Activity represents the "get recipient data list for teams" durable activity.
    /// 1). It takes the recipient data list of teams ("team general channels") as passing in parameter.
    /// 2). It breaks that list of teams into batches.
    /// 3). For each batch:
    ///         It initializes the sent notification data table with a row for each team.
    ///         It initializes a partitioned batch for that batch in the send batches data.
    /// </summary>
    public class GetRecipientDataListForTeamsActivity
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly SendBatchesDataRepository sendBatchesDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataListForTeamsActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="sendBatchesDataRepository">The send batches data repository.</param>
        public GetRecipientDataListForTeamsActivity(
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
        /// <param name="teamDataEntities">The team data entity list.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<RecipientDataListInformation> RunAsync(
            IDurableOrchestrationContext context,
            IEnumerable<TeamDataEntity> teamDataEntities,
            NotificationDataEntity notificationDataEntity)
        {
            var recipientDataListInformation = await context.CallActivityWithRetryAsync<RecipientDataListInformation>(
                nameof(GetRecipientDataListForTeamsActivity.GetTeamRecipientDataListAsync),
                ActivitySettings.CommonActivityRetryOptions,
                (notificationDataEntity.Id, teamDataEntities));

            return recipientDataListInformation;
        }

        /// <summary>
        /// This method represents the "get recipient data list for teams" durable activity.
        /// 1). It takes the recipient data list of teams ("team general channels") as passing in parameter.
        /// 2). It breaks that list of teams into batches.
        /// 3). For each batch:
        ///         It initializes the sent notification data table with a row for each team.
        ///         It initializes a partitioned batch for that batch in the send batches data.
        /// </summary>
        /// <param name="dto">An instance of GetRecipientDataListForTeamsActivityDTO.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(GetTeamRecipientDataListAsync))]
        public async Task<RecipientDataListInformation> GetTeamRecipientDataListAsync(
            [ActivityTrigger]
            (string NotificationDataEntityId, IEnumerable<TeamDataEntity> TeamDataEntityList) dto)
        {
            var teamDataEntities = dto.TeamDataEntityList;

            var teamDataEntityList = teamDataEntities.ToList();
            var teamDataEntityBatches = teamDataEntityList.SeparateIntoBatches();

            var recipientDataListInformation = new RecipientDataListInformation
            {
                TotalNumberOfRecipients = teamDataEntityList.Count,
                NumberOfRecipientDataBatches = teamDataEntityBatches.Count,
            };

            var batchIndex = 1;
            foreach (var teamDataEntityBatch in teamDataEntityBatches)
            {
                // Create a new separate batch to store in each data table.
                var sentNotificationDataRepositoryBatch = new List<SentNotificationDataEntity>();
                var sendBatchesDataRepositoryBatch = new List<SentNotificationDataEntity>();

                // Get the batch partition key to be used for every entity in the current batch.
                var batchPartitionKey = this.sendBatchesDataRepository.GetBatchPartitionKey(
                    notificationId: dto.NotificationDataEntityId,
                    batchIndex: batchIndex);

                // Iterate the teams in the batch and create the appropriate entity to
                // store in each data table.
                foreach (var teamDataEntity in teamDataEntityBatch)
                {
                    // The partition key for the SentNotificationData table is just the notification Id.
                    sentNotificationDataRepositoryBatch.Add(
                        teamDataEntity.CreateInitialSentNotificationDataEntity(
                            partitionKey: dto.NotificationDataEntityId));

                    // The SendBatchesData table is separated into batches based on the partition key, so
                    // the batchPartitionKey is used here.
                    sendBatchesDataRepositoryBatch.Add(
                        teamDataEntity.CreateInitialSentNotificationDataEntity(
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
