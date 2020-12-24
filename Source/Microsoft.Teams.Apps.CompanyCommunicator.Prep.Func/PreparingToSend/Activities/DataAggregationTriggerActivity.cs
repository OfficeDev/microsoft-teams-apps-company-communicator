// <copyright file="DataAggregationTriggerActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;

    /// <summary>
    /// Data aggregation trigger activity.
    ///
    /// Does following:
    /// 1. Updates notification (total recipient count).
    /// 2. Sends message to data queue.
    /// </summary>
    public class DataAggregationTriggerActivity
    {
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly IDataQueue dataQueue;
        private readonly double messageDelayInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAggregationTriggerActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        /// <param name="dataQueue">The data queue.</param>
        /// <param name="options">The data queue message options.</param>
        public DataAggregationTriggerActivity(
            INotificationDataRepository notificationDataRepository,
            IDataQueue dataQueue,
            IOptions<DataQueueMessageOptions> options)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
            this.messageDelayInSeconds = options?.Value?.MessageDelayInSeconds ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Does following:
        /// 1. Updates notification (total recipient count).
        /// 2. Sends message to data queue.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="log">logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.DataAggregationTriggerActivity)]
        public async Task RunAsync(
            [ActivityTrigger](string notificationId, int recipientCount) input,
            ILogger log)
        {
            if (input.notificationId == null)
            {
                throw new ArgumentNullException(nameof(input.notificationId));
            }

            if (input.recipientCount <= 0)
            {
                throw new ArgumentOutOfRangeException($"Recipient count should be > 0. Value: {input.recipientCount}");
            }

            // Update notification.
            await this.UpdateNotification(input.notificationId, input.recipientCount, log);

            // Send message to data queue.
            await this.SendMessageToDataQueue(input.notificationId);
        }

        /// <summary>
        /// Update notification data (total recipient count).
        /// </summary>
        /// <param name="notificationId">Notification id.</param>
        /// <param name="recipientCount">Recipient count.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task UpdateNotification(string notificationId, int recipientCount, ILogger log)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                notificationId);

            if (notificationDataEntity == null)
            {
                log.LogError($"Notification entity not found. Notification Id: {notificationId}");
                return;
            }

            notificationDataEntity.TotalMessageCount = recipientCount;

            await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
        }

        /// <summary>
        /// Sends message to data queue to trigger Data function.
        /// </summary>
        /// <param name="notificationId">Notification id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendMessageToDataQueue(string notificationId)
        {
            var dataQueueMessageContent = new DataQueueMessageContent
            {
                NotificationId = notificationId,
                ForceMessageComplete = false,
            };

            await this.dataQueue.SendDelayedAsync(
                dataQueueMessageContent,
                this.messageDelayInSeconds);
        }
    }
}
