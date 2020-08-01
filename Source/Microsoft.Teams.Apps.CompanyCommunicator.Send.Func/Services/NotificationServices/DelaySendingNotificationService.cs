// <copyright file="DelaySendingNotificationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// A service for handling messages that need to be delayed and retried due to the system being throttled.
    /// </summary>
    public class DelaySendingNotificationService
    {
        private readonly GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository;
        private readonly SendQueue sendQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelaySendingNotificationService"/> class.
        /// </summary>
        /// <param name="globalSendingNotificationDataRepository">The global sending notification data repository.</param>
        /// <param name="sendQueue">The send queue.</param>
        public DelaySendingNotificationService(
            GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository,
            SendQueue sendQueue)
        {
            this.globalSendingNotificationDataRepository = globalSendingNotificationDataRepository;
            this.sendQueue = sendQueue;
        }

        /// <summary>
        /// This method sets the globally accessible delay time indicating to all other functions that the system is currently in a
        /// throttled state and all messages need to be delayed and sends the queue message back to the queue with a delayed wait time.
        /// </summary>
        /// <param name="sendRetryDelayNumberOfSeconds">The number of seconds for the system and message to be delayed.</param>
        /// <param name="sendQueueMessageContent">The send queue message content to be sent back to the send queue for a delayed retry.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DelaySendingNotificationAsync(
            double sendRetryDelayNumberOfSeconds,
            SendQueueMessageContent sendQueueMessageContent,
            ILogger log)
        {
            try
            {
                // Shorten this time by 15 seconds to ensure that when the delayed retry message is taken off of the queue
                // the Send Retry Delay Time will be earlier and will not block it
                var sendRetryDelayTime = DateTime.UtcNow + TimeSpan.FromSeconds(sendRetryDelayNumberOfSeconds - 15);

                var globalSendingNotificationDataEntity = new GlobalSendingNotificationDataEntity
                {
                    SendRetryDelayTime = sendRetryDelayTime,
                };

                var setGlobalSendingNotificationDataEntityTask = this.globalSendingNotificationDataRepository
                    .SetGlobalSendingNotificationDataEntityAsync(globalSendingNotificationDataEntity);

                var sendDelayedRetryTask = this.sendQueue.SendDelayedAsync(sendQueueMessageContent, sendRetryDelayNumberOfSeconds);

                await Task.WhenAll(
                    setGlobalSendingNotificationDataEntityTask,
                    sendDelayedRetryTask);
            }
            catch (Exception e)
            {
                log.LogError(e, $"ERROR: {e.GetType()}: {e.Message}");
                throw;
            }
        }
    }
}
