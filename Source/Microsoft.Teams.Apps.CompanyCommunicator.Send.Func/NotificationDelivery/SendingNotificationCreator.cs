// <copyright file="SendingNotificationCreator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.NotificationDelivery
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// Sending notification creator.
    /// </summary>
    public class SendingNotificationCreator
    {
        private readonly string microsoftAppId;
        private readonly string microsoftAppPassword;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendingNotificationCreator"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="notificationDataRepository">Notification Repository instance.</param>
        /// <param name="sendingNotificationDataRepository">Sending notification data repository.</param>
        /// <param name="adaptiveCardCreator">The adaptive card creator.</param>
        public SendingNotificationCreator(
            IConfiguration configuration,
            NotificationDataRepository notificationDataRepository,
            SendingNotificationDataRepository sendingNotificationDataRepository,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.microsoftAppId = configuration["MicrosoftAppId"];
            this.microsoftAppPassword = configuration["MicrosoftAppPassword"];
            this.notificationDataRepository = notificationDataRepository;
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.adaptiveCardCreator = adaptiveCardCreator;
        }

        /// <summary>
        /// Generate an adaptive card in json.
        /// </summary>
        /// <param name="rowKey">The row key.</param>
        /// <param name="notificationEntity">The notification entity to create as a draft.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task CreateAsync(string rowKey, NotificationDataEntity notificationEntity)
        {
            var cardString = this.adaptiveCardCreator.CreateAdaptiveCard(notificationEntity).ToJson();

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                RowKey = rowKey,
                NotificationId = rowKey,
                Content = cardString,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
