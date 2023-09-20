// <copyright file="ScheduleSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;

    /// <summary>
    /// Azure Function Timer triggered app  which gets executed every 5 minutes.
    /// Used for sending the scheduled messages from the bot.
    /// </summary>
    public class ScheduleSendFunction
    {
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IPrepareToSendQueue prepareToSendQueue;
        private readonly IDataQueue dataQueue;
        private readonly double forceCompleteMessageDelayInSeconds = 86400;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleSendFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository service that deals with the table storage in azure.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="prepareToSendQueue">The service bus queue for preparing to send notifications.</param>
        /// <param name="dataQueue">The service bus queue for the data queue.</param>
        public ScheduleSendFunction(
            INotificationDataRepository notificationDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository,
            IPrepareToSendQueue prepareToSendQueue,
            IDataQueue dataQueue)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.prepareToSendQueue = prepareToSendQueue ?? throw new ArgumentNullException(nameof(prepareToSendQueue));
            this.dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
        }

        /// <summary>
        /// Azure Function App Timer triggered.
        /// Used for triggering to send the scheduled messages from Azure Table storage.
        /// </summary>
        /// <param name="myTimer">The timer schedule.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("ScheduleSendFunction")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"ScheduleSendFunction timer triggered executed at: {DateTime.Now}");

                var tasks = new List<Task>();
                var notificationEntities = await this.notificationDataRepository.GetAllPendingScheduledNotificationsAsync();
                foreach (var scheduledDraft in notificationEntities)
                {
                    var draftNotificationDataEntity = await this.notificationDataRepository.GetAsync(
                        NotificationDataTableNames.DraftNotificationsPartition,
                        scheduledDraft.Id);

                    log.LogInformation($"draft.Id and ScheduledDateTime {draftNotificationDataEntity.Id}-{draftNotificationDataEntity.Title}....{draftNotificationDataEntity.ScheduledDate}");
                    if (draftNotificationDataEntity.ScheduledDate <= DateTime.Now)
                    {
                        log.LogInformation($"that means scheduled Date Time less than now: {draftNotificationDataEntity.ScheduledDate}");

                        var newSentNotificationId =
                        await this.notificationDataRepository.MoveDraftToSentPartitionAsync(draftNotificationDataEntity);
                        log.LogInformation($"newSentNotificationId {newSentNotificationId}");

                        tasks.Add(this.SendScheduledNotifications(newSentNotificationId));
                        tasks.Add(this.ForceSendScheduleNotifications(newSentNotificationId));
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                var errorMessage = $"ScheduleSendFunction failed to run. Exception Message: {ex.Message}";
                log.LogError(ex, errorMessage);
            }
        }

        /// <summary>
        /// This queues the message to prepare to send queue in service bus.
        /// </summary>
        /// <param name="sentNotificationId">The new sent notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendScheduledNotifications(string sentNotificationId)
        {
            // Ensure the data table needed by the Azure Functions to send the notifications exist in Azure storage.
            await this.sentNotificationDataRepository.EnsureSentNotificationDataTableExistsAsync();

            var prepareToSendQueueMessageContent = new PrepareToSendQueueMessageContent
            {
                NotificationId = sentNotificationId,
            };
            await this.prepareToSendQueue.SendAsync(prepareToSendQueueMessageContent);
        }

        /// <summary>
        /// This sends a force complete message to data queue in service bus.
        /// </summary>
        /// <param name="sentNotificationId">The new sent notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ForceSendScheduleNotifications(string sentNotificationId)
        {
            // Send a "force complete" message to the data queue with a delay to ensure that
            // the notification will be marked as complete no matter the counts
            var forceCompleteDataQueueMessageContent = new DataQueueMessageContent
            {
                NotificationId = sentNotificationId,
                ForceMessageComplete = true,
            };
            await this.dataQueue.SendDelayedAsync(
                forceCompleteDataQueueMessageContent,
                this.forceCompleteMessageDelayInSeconds);
        }
    }
}