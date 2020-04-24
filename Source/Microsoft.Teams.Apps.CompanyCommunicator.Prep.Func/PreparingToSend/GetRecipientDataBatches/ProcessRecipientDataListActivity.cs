// <copyright file="ProcessRecipientDataListActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// This class contains the "process recipient data list" durable activity.
    /// This activity pulls all of the SentNotification data table entries, converts
    /// them to recipient data objects, counts and stores the total number of recipients,
    /// and breaks the total group of recipients into batches to be sent to the send queue.
    /// </summary>
    public class ProcessRecipientDataListActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessRecipientDataListActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        public ProcessRecipientDataListActivity(
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <returns>Recipient data batches.</returns>
        public async Task<IEnumerable<IEnumerable<RecipientData>>> RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId)
        {
            var recipientDataBatches =
                await context.CallActivityWithRetryAsync<IEnumerable<IEnumerable<RecipientData>>>(
                    nameof(ProcessRecipientDataListActivity.ProcessRecipientDataListAsync),
                    ActivitySettings.CommonActivityRetryOptions,
                    notificationDataEntityId);

            return recipientDataBatches;
        }

        /// <summary>
        /// This method represents the "process recipient data list" activity.
        /// It processes incoming "recipient data list" as follows.
        /// 1). Load sent notification data.
        /// 2). Update total recipient count in notification data entity.
        /// 3). Page the recipient data list.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(ProcessRecipientDataListAsync))]
        public async Task<IEnumerable<IEnumerable<RecipientData>>> ProcessRecipientDataListAsync(
            [ActivityTrigger] string notificationDataEntityId)
        {
            var sentNotificationDataEntityList =
                await this.sentNotificationDataRepository.GetAllAsync(notificationDataEntityId);

            // Fill the recipient list with recipient data based on the type of the recipient it
            // is (as stored in the SentNotificationDataEntity) and the data stored in the
            // SentNotificationDataEntity.
            var recipientDataList = new List<RecipientData>();
            foreach (var sentNotificationDataEntity in sentNotificationDataEntityList)
            {
                if (sentNotificationDataEntity.RecipientType
                    == SentNotificationDataEntity.UserRecipientType)
                {
                    recipientDataList.Add(new RecipientData
                    {
                        RecipientType = RecipientDataType.User,
                        UserData = new UserDataEntity
                        {
                            AadId = sentNotificationDataEntity.RecipientId,
                            UserId = sentNotificationDataEntity.UserId,
                            ConversationId = sentNotificationDataEntity.ConversationId,
                            ServiceUrl = sentNotificationDataEntity.ServiceUrl,
                            TenantId = sentNotificationDataEntity.TenantId,
                        },
                    });
                }
                else if (sentNotificationDataEntity.RecipientType
                    == SentNotificationDataEntity.TeamRecipientType)
                {
                    recipientDataList.Add(new RecipientData
                    {
                        RecipientType = RecipientDataType.Team,
                        TeamData = new TeamDataEntity
                        {
                            TeamId = sentNotificationDataEntity.RecipientId,
                            ServiceUrl = sentNotificationDataEntity.ServiceUrl,
                            TenantId = sentNotificationDataEntity.TenantId,
                        },
                    });
                }
            }

            await this.SetTotalRecipientCountInNotificationDataAsync(
                notificationDataEntityId,
                recipientDataList.Count);

            var recipientDataBatches = this.CreateRecipientDataBatches(recipientDataList);
            return recipientDataBatches;
        }

        /// <summary>
        /// Set total recipient count in notification data entity.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="totalExpectedRecipientCount">The total number of expected recipients.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private async Task SetTotalRecipientCountInNotificationDataAsync(
            string notificationDataEntityId,
            int totalExpectedRecipientCount)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.TotalMessageCount = totalExpectedRecipientCount;

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }

        private IEnumerable<IEnumerable<RecipientData>> CreateRecipientDataBatches(
            List<RecipientData> recipientDataList)
        {
            var recipientDataBatches = new List<List<RecipientData>>();

            var totalNumberOfRecipients = recipientDataList.Count;

            // Use the SendQueue's maximum number of messages in a batch request number because
            // the list is being broken into batches in order to be added to that queue.
            var batchSize = SendQueue.MaxNumberOfMessagesInBatchRequest;
            var numberOfCompleteBatches = totalNumberOfRecipients / batchSize;
            var numberRecipientsInIncompleteBatch = totalNumberOfRecipients % batchSize;

            for (var i = 0; i < numberOfCompleteBatches; i++)
            {
                var startingIndex = i * batchSize;
                var batch = recipientDataList.GetRange(startingIndex, batchSize);
                recipientDataBatches.Add(batch);
            }

            if (numberRecipientsInIncompleteBatch != 0)
            {
                var incompleteBatchStartingIndex = numberOfCompleteBatches * batchSize;
                var incompleteBatch = recipientDataList.GetRange(
                    incompleteBatchStartingIndex,
                    numberRecipientsInIncompleteBatch);
                recipientDataBatches.Add(incompleteBatch);
            }

            return recipientDataBatches;
        }
    }
}
