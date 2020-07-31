// <copyright file="PrecheckService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.PrecheckServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// Service to check if the data queue message should be processed. Scenarios it checks for are:
    ///     If the entire system is currently in a throttled state.
    ///     If the notification has already been attempted to be sent to this recipient.
    /// </summary>
    public class PrecheckService
    {
        private readonly GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly SendQueue sendQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecheckService"/> class.
        /// </summary>
        /// <param name="globalSendingNotificationDataRepository">The global sending notification data repository.</param>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        /// <param name="sendQueue">The send queue.</param>
        public PrecheckService(
            GlobalSendingNotificationDataRepository globalSendingNotificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository,
            SendQueue sendQueue)
        {
            this.globalSendingNotificationDataRepository = globalSendingNotificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.sendQueue = sendQueue;
        }

        /// <summary>
        /// Checks if the data queue message should be processed. Scenarios it checks for are:
        ///     If the entire system is currently in a throttled state.
        ///     If the notification has already been attempted to be sent to this recipient.
        /// </summary>
        /// <param name="messageContent">The data queue message content.</param>
        /// <param name="sendRetryDelayNumberOfSeconds">The send retry delay number of seconds.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> VerifyMessageShouldBeProcessedAsync(
            SendQueueMessageContent messageContent,
            double sendRetryDelayNumberOfSeconds,
            ILogger log)
        {
            try
            {
                // Fetch the current global sending notification data. This is where data about the overall system's
                // status is stored e.g. is everything in a delayed state because the bot is being throttled.
                var globalSendingNotificationDataEntityTask = this.globalSendingNotificationDataRepository
                    .GetGlobalSendingNotificationDataEntityAsync();

                // Fetch the sent notification data to verify it has only been initialized and a notification
                // has not already been sent to this recipient.
                var existingSentNotificationDataEntityTask = this.sentNotificationDataRepository
                    .GetAsync(
                        partitionKey: messageContent.NotificationId,
                        rowKey: messageContent.RecipientData.RecipientId);

                await Task.WhenAll(
                    globalSendingNotificationDataEntityTask,
                    existingSentNotificationDataEntityTask);

                var globalSendingNotificationDataEntity = await globalSendingNotificationDataEntityTask;
                var existingSentNotificationDataEntity = await existingSentNotificationDataEntityTask;

                var shouldProceedWithProcessing = true;

                // If the overall system is in a throttled state and needs to be delayed,
                // add the message back on the queue with a delay and stop processing the queue message.
                if (globalSendingNotificationDataEntity?.SendRetryDelayTime != null
                    && DateTime.UtcNow < globalSendingNotificationDataEntity.SendRetryDelayTime)
                {
                    await this.sendQueue.SendDelayedAsync(messageContent, sendRetryDelayNumberOfSeconds);

                    shouldProceedWithProcessing = false;
                }

                // First, verify that the recipient's sent notification data has been stored and initialized. This
                // verifies a notification is expected to be sent to this recipient.
                // Next, in order to not send a duplicate notification to this recipient, verify that the StatusCode
                // in the sent notification data is set to either:
                //      The InitializationStatusCode (likely 0) - this means the notification has not been attempted
                //          to be sent to this recipient.
                //      The FaultedAndRetryingStatusCode (likely -1) - this means the Azure Function previously attempted
                //          to send the notification to this recipient but threw an exception, so sending the
                //          notification should be attempted again.
                // If it is neither of these scenarios, then complete the function in order to not send a duplicate
                // notification to this recipient.
                else if (existingSentNotificationDataEntity == null
                    || (existingSentNotificationDataEntity.StatusCode != SentNotificationDataEntity.InitializationStatusCode
                        && existingSentNotificationDataEntity.StatusCode != SentNotificationDataEntity.FaultedAndRetryingStatusCode))
                {
                    shouldProceedWithProcessing = false;
                }

                return shouldProceedWithProcessing;
            }
            catch (Exception e)
            {
                var errorMessage = $"{e.GetType()}: {e.Message}";
                log.LogError(e, $"ERROR: {errorMessage}");
                throw;
            }
        }
    }
}
