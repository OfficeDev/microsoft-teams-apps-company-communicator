// <copyright file="NotificationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// Notification Service.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IGlobalSendingNotificationDataRepository globalSendingNotificationDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="globalSendingNotificationDataRepository">The global sending notification data repository.</param>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public NotificationService(
            IGlobalSendingNotificationDataRepository globalSendingNotificationDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository)
        {
            this.globalSendingNotificationDataRepository = globalSendingNotificationDataRepository ?? throw new ArgumentNullException(nameof(globalSendingNotificationDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
        }

        /// <inheritdoc/>
        public async Task<bool> IsSendNotificationThrottled()
        {
            var globalNotificationStatus = await this.globalSendingNotificationDataRepository.GetGlobalSendingNotificationDataEntityAsync();
            if (globalNotificationStatus?.SendRetryDelayTime == null)
            {
                return false;
            }

            return globalNotificationStatus.SendRetryDelayTime > DateTime.UtcNow;
        }

        /// <inheritdoc/>
        public async Task<bool> IsPendingNotification(SendQueueMessageContent message)
        {
            var recipient = message?.RecipientData;
            if (string.IsNullOrWhiteSpace(recipient?.RecipientId))
            {
                throw new InvalidOperationException("Recipient id is not set.");
            }

            // Check notification status for the recipient.
            var notification = await this.sentNotificationDataRepository.GetAsync(
                partitionKey: message.NotificationId,
                rowKey: message.RecipientData.RecipientId);

            // To avoid sending duplicate messages, we check if the Status code is either of the following:
            // 1. InitializationStatusCode: this means the notification has not been attempted to be sent to this recipient.
            // 2. FaultedAndRetryingStatusCode: this means the Azure Function previously attempted to send the notification
            //    to this recipient but failed and should be retried.
            if (notification?.StatusCode == SentNotificationDataEntity.InitializationStatusCode ||
                notification?.StatusCode == SentNotificationDataEntity.FaultedAndRetryingStatusCode)
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task SetSendNotificationThrottled(double sendRetryDelayNumberOfSeconds)
        {
            // Ensure global retry timestamp is less re-queue delay time for the message.
            var globalSendingNotificationDataEntity = new GlobalSendingNotificationDataEntity
            {
                SendRetryDelayTime = DateTime.UtcNow + TimeSpan.FromSeconds(sendRetryDelayNumberOfSeconds - 15),
            };

            await this.globalSendingNotificationDataRepository
                .SetGlobalSendingNotificationDataEntityAsync(globalSendingNotificationDataEntity);
        }

        /// <inheritdoc/>
        public async Task UpdateSentNotification(
            string notificationId,
            string recipientId,
            int totalNumberOfSendThrottles,
            int statusCode,
            string allSendStatusCodes,
            string errorMessage)
        {
            // Current time as sent date time.
            var sentDateTime = DateTime.UtcNow;

            var notification = await this.sentNotificationDataRepository.GetAsync(
                partitionKey: notificationId,
                rowKey: recipientId);

            // Update notification.
            notification.TotalNumberOfSendThrottles = totalNumberOfSendThrottles;
            notification.SentDate = sentDateTime;
            notification.IsStatusCodeFromCreateConversation = false;
            notification.StatusCode = (int)statusCode;
            notification.ErrorMessage = errorMessage;
            notification.NumberOfFunctionAttemptsToSend = notification.NumberOfFunctionAttemptsToSend + 1;
            notification.AllSendStatusCodes = $"{notification.AllSendStatusCodes ?? string.Empty}{allSendStatusCodes}";

            if (statusCode == (int)HttpStatusCode.Created)
            {
                notification.DeliveryStatus = SentNotificationDataEntity.Succeeded;
            }
            else if (statusCode == (int)HttpStatusCode.TooManyRequests)
            {
                notification.DeliveryStatus = SentNotificationDataEntity.Throttled;
            }
            else if (statusCode == (int)HttpStatusCode.NotFound)
            {
                notification.DeliveryStatus = SentNotificationDataEntity.RecipientNotFound;
            }
            else if (statusCode == SentNotificationDataEntity.FaultedAndRetryingStatusCode)
            {
                notification.DeliveryStatus = SentNotificationDataEntity.Retrying;
            }
            else
            {
                notification.DeliveryStatus = SentNotificationDataEntity.Failed;
            }

            await this.sentNotificationDataRepository.InsertOrMergeAsync(notification);
        }
    }
}
