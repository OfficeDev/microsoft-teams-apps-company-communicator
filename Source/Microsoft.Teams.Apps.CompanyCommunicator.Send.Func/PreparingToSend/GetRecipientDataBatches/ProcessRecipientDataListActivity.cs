// <copyright file="ProcessRecipientDataListActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Initialize sent notification data entities in Azure table storage.
    /// </summary>
    public class ProcessRecipientDataListActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessRecipientDataListActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public ProcessRecipientDataListActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataList">Recipient data list.</param>
        /// <returns>Recipient data batches.</returns>
        public async Task<IEnumerable<IEnumerable<UserDataEntity>>> RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataList)
        {
            var recipientDataBatches =
                await context.CallActivityAsync<IEnumerable<IEnumerable<UserDataEntity>>>(
                    nameof(ProcessRecipientDataListActivity.ProcessRecipientDataListAsync),
                    new ProcessRecipientDataListActivityDTO
                    {
                        NotificationDataEntityId = notificationDataEntityId,
                        RecipientDataList = recipientDataList,
                    });

            return recipientDataBatches;
        }

        /// <summary>
        /// Initialize sent notification data entities in Azure table storage.
        /// This function includes the following actions:
        /// 1). Deduplicate recipient data.
        /// 2). Set status in sent notification data in the table storage.
        /// 3). Update total recipient count in notification data entity.
        /// 4). Page the recipient data list.
        /// </summary>
        /// <param name="input">Input data.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(ProcessRecipientDataListAsync))]
        public async Task<IEnumerable<IEnumerable<UserDataEntity>>> ProcessRecipientDataListAsync(
            [ActivityTrigger] ProcessRecipientDataListActivityDTO input)
        {
            var deduplicated = new RecipientDataEntityHashSet(input.RecipientDataList);

            await this.metadataProvider.InitializeStatusInSentNotificationDataAsync(
                input.NotificationDataEntityId,
                deduplicated);

            await this.metadataProvider.SetTotalRecipientCountInNotificationDataAsync(
                input.NotificationDataEntityId,
                deduplicated);

            var paged = this.CreateRecipientDataBatches(deduplicated.ToList());
            return paged;
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
