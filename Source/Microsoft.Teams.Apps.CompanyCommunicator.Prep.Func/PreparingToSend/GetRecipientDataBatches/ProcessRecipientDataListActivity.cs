// <copyright file="ProcessRecipientDataListActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "process recipient data list" durable activity.
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
        public async Task<IEnumerable<IEnumerable<UserDataEntity>>> RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId)
        {
            var recipientDataBatches =
                await context.CallActivityWithRetryAsync<IEnumerable<IEnumerable<UserDataEntity>>>(
                    nameof(ProcessRecipientDataListActivity.ProcessRecipientDataListAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
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
        public async Task<IEnumerable<IEnumerable<UserDataEntity>>> ProcessRecipientDataListAsync(
            [ActivityTrigger] string notificationDataEntityId)
        {
            var sentNotificationDataEntityList =
                await this.sentNotificationDataRepository.GetAllAsync(notificationDataEntityId);
            var recipientDataList = sentNotificationDataEntityList
                .Where(p => p.StatusCode == 0)
                .Select(p =>
                    new UserDataEntity
                    {
                        AadId = p.AadId,
                        UserId = p.UserId,
                        ConversationId = p.ConversationId,
                        ServiceUrl = p.ServiceUrl,
                        TenantId = p.TenantId,
                    });

            await this.SetTotalRecipientCountInNotificationDataAsync(
                notificationDataEntityId,
                recipientDataList);

            var paged = this.CreateRecipientDataBatches(recipientDataList.ToList());
            return paged;
        }

        /// <summary>
        /// Set total recipient count in notification data entity.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataList">Recipient data list.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        internal async Task SetTotalRecipientCountInNotificationDataAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataList)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.TotalMessageCount = recipientDataList.Count();

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }

        private IEnumerable<IEnumerable<UserDataEntity>> CreateRecipientDataBatches(
            List<UserDataEntity> recipientDataList)
        {
            var recipientDataBatches = new List<List<UserDataEntity>>();

            var totalNumberOfRecipients = recipientDataList.Count;
            var batchSize = 100;
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
