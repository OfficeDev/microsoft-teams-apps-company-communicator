// <copyright file="ProcessAndStoreMessageActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
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
    /// Process message activity.
    ///
    /// Prepares the message from notification entity and stores the information in sending notification data table.
    /// </summary>
    public class ProcessAndStoreMessageActivity
    {
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessAndStoreMessageActivity"/> class.
        /// </summary>
        /// <param name="notificationRepo">Sending notification data repository.</param>
        /// <param name="cardCreator">The adaptive card creator.</param>
        public ProcessAndStoreMessageActivity(
            SendingNotificationDataRepository notificationRepo,
            AdaptiveCardCreator cardCreator)
        {
            this.sendingNotificationDataRepository = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
            this.adaptiveCardCreator = cardCreator ?? throw new ArgumentNullException(nameof(cardCreator));
        }

        /// <summary>
        /// Durable activity function.
        ///
        /// Prepares serialzied message content and stores the message in sending notification data table.
        /// </summary>
        /// <param name="notificationDataEntity">A notification to be sent to recipients.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(FunctionNames.ProcessAndStoreMessageActivity)]
        public async Task RunAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var serializedContent = this.adaptiveCardCreator.CreateAdaptiveCard(notificationDataEntity).ToJson();

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = NotificationDataTableNames.SendingNotificationsPartition,
                RowKey = notificationDataEntity.RowKey,
                NotificationId = notificationDataEntity.Id,
                Content = serializedContent,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
