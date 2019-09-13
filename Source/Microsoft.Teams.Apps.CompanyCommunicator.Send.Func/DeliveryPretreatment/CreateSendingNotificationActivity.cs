// <copyright file="CreateSendingNotificationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// Create sending notification activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class CreateSendingNotificationActivity
    {
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSendingNotificationActivity"/> class.
        /// </summary>
        /// <param name="sendingNotificationDataRepository">Sending notification data repository.</param>
        /// <param name="adaptiveCardCreator">The adaptive card creator.</param>
        public CreateSendingNotificationActivity(
            SendingNotificationDataRepository sendingNotificationDataRepository,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.adaptiveCardCreator = adaptiveCardCreator;
        }

        /// <summary>
        /// Generate an adaptive card in json.
        /// </summary>
        /// <param name="notificationEntity">The notification entity to create as a draft.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(CreateSendingNotificationAsync))]
        public async Task CreateSendingNotificationAsync(
            [ActivityTrigger] NotificationDataEntity notificationEntity)
        {
            var cardString = this.adaptiveCardCreator.CreateAdaptiveCard(notificationEntity).ToJson();

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                RowKey = notificationEntity.RowKey,
                NotificationId = notificationEntity.RowKey,
                Content = cardString,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
