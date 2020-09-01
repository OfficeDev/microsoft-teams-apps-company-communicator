// <copyright file="SendBatchMessagesActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;

    /// <summary>
    /// This Activity represents the Send batch messages to send queue activity.
    /// Ultimately this activity sends a batch of queue messages to the Send queue.
    /// 1) It pulls the batch corresponding with the notification Id and the
    ///     recipient data batch index from the send batches data table.
    /// 2) It transforms that data into the appropriate content for the Send
    ///     queue.
    /// 3) It sends those Send queue triggers in one batch request to the Service
    ///     Bus Send queue so they reach the Azure Send function.
    /// </summary>
    public class SendBatchMessagesActivity
    {
        private readonly SendBatchesDataRepository sendBatchesDataRepository;
        private readonly SendQueue sendQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendBatchMessagesActivity"/> class.
        /// </summary>
        /// <param name="sendBatchesDataRepository">The send batches data repository.</param>
        /// <param name="sendQueue">Send queue service.</param>
        public SendBatchMessagesActivity(
            SendBatchesDataRepository sendBatchesDataRepository,
            SendQueue sendQueue)
        {
            this.sendBatchesDataRepository = sendBatchesDataRepository ?? throw new ArgumentNullException(nameof(sendBatchesDataRepository));
            this.sendQueue = sendQueue ?? throw new ArgumentNullException(nameof(sendQueue));
        }

        /// <summary>
        /// This method represents the Send batch messages to send queue activity.
        /// 1) It pulls the batch corresponding with the notification Id and the
        ///     recipient data batch index from the send batches data table.
        /// 2) It transforms that data into the appropriate content for the Send
        ///     queue.
        /// 3) It sends those Send queue triggers in one batch request to the Service
        ///     Bus Send queue so they reach the Azure Send function.
        /// </summary>
        /// <param name="tuple">Tuple.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.SendBatchMessagesActivity)]
        public async Task RunAsync(
            [ActivityTrigger](string notificationId, int recipientDataBatchIndex) tuple)
        {
            var notificationId = tuple.notificationId;
            var recipientDataBatchIndex = tuple.recipientDataBatchIndex;

            var batchPartitionKey = this.sendBatchesDataRepository.GetBatchPartitionKey(
                notificationId: notificationId,
                batchIndex: recipientDataBatchIndex);

            var sentNotificationDataEntityBatch = await this.sendBatchesDataRepository.GetAllAsync(
                partition: batchPartitionKey);

            // Fill the recipient data batch with recipient data based on the type of the recipient it
            // is (as stored in the SentNotificationDataEntity) and the data stored in the
            // SentNotificationDataEntity.
            var recipientDataBatch = new List<RecipientData>();
            foreach (var sentNotificationDataEntity in sentNotificationDataEntityBatch)
            {
                if (sentNotificationDataEntity.RecipientType
                    == SentNotificationDataEntity.UserRecipientType)
                {
                    recipientDataBatch.Add(new RecipientData
                    {
                        RecipientType = RecipientDataType.User,
                        RecipientId = sentNotificationDataEntity.RecipientId,
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
                    recipientDataBatch.Add(new RecipientData
                    {
                        RecipientType = RecipientDataType.Team,
                        RecipientId = sentNotificationDataEntity.RecipientId,
                        TeamData = new TeamDataEntity
                        {
                            TeamId = sentNotificationDataEntity.RecipientId,
                            ServiceUrl = sentNotificationDataEntity.ServiceUrl,
                            TenantId = sentNotificationDataEntity.TenantId,
                        },
                    });
                }
            }

            var sendQueueMessageContentBatch = recipientDataBatch
                .Select(recipientData =>
                    new SendQueueMessageContent
                    {
                        NotificationId = notificationId,
                        RecipientData = recipientData,
                    });

            await this.sendQueue.SendAsync(sendQueueMessageContentBatch);
        }
    }
}
