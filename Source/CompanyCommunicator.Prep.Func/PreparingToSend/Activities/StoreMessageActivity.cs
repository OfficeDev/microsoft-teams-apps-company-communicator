// <copyright file="StoreMessageActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// Stores the message in sending notification data table.
    /// </summary>
    public class StoreMessageActivity
    {
        private readonly ISendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreMessageActivity"/> class.
        /// </summary>
        /// <param name="notificationRepo">Sending notification data repository.</param>
        /// <param name="cardCreator">The adaptive card creator.</param>
        public StoreMessageActivity(
            ISendingNotificationDataRepository notificationRepo,
            AdaptiveCardCreator cardCreator)
        {
            this.sendingNotificationDataRepository = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
            this.adaptiveCardCreator = cardCreator ?? throw new ArgumentNullException(nameof(cardCreator));
        }

        /// <summary>
        /// Stores the message in sending notification data table.
        /// </summary>
        /// <param name="notification">A notification to be sent to recipients.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(FunctionNames.StoreMessageActivity)]
        public async Task RunAsync(
            [ActivityTrigger] NotificationDataEntity notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            // In case we have blob name instead of URL to public image.
            if (!string.IsNullOrEmpty(notification.ImageBase64BlobName)
                && notification.ImageLink.StartsWith(Common.Constants.ImageBase64Format))
            {
                notification.ImageLink += await this.sendingNotificationDataRepository.GetImageAsync(notification.ImageBase64BlobName);
            }

            var serializedContent = this.adaptiveCardCreator.CreateAdaptiveCard(notification).ToJson();

            // Save Adaptive Card with data uri into blob storage. Blob name = notification.Id.
            await this.sendingNotificationDataRepository.SaveAdaptiveCardAsync(notification.Id, serializedContent);

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = NotificationDataTableNames.SendingNotificationsPartition,
                RowKey = notification.RowKey,
                NotificationId = notification.Id,
                Content = notification.Id,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
