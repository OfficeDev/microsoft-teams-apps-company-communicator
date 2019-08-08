// <copyright file="ActiveNotificationCreator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ActiveNotification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;

    /// <summary>
    /// Active Notification creator.
    /// </summary>
    public class ActiveNotificationCreator
    {
        private static readonly string AdaptiveCardTemplate = @"
            {
              'type': 'AdaptiveCard',
              'body': [
                {
                  'type': 'TextBlock',
                  'weight': 'Bolder',
                  'text': 'Title',
                  'size': 'ExtraLarge',
                  'wrap': true
                },
                {
                  'type': 'Image',
                  'spacing': 'Default',
                  'url': '',
                  'size': 'Stretch',
                  'width': '400px',
                  'altText': ''
                },
                {
                  'type': 'TextBlock',
                  'text': '',
                  'wrap': true
                },
                {
                  'type': 'TextBlock',
                  'wrap': true,
                  'size': 'Small',
                  'weight': 'Lighter',
                  'text': 'Sent by: Anonymous'
                }
              ],
              '$schema': 'http://adaptivecards.io/schemas/adaptive-card.json',
              'version': '1.0'
            }
        ";

        private readonly NotificationRepository notificationRepository;
        private readonly ActiveNotificationRepository activeNotificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveNotificationCreator"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification Repository instance.</param>
        /// <param name="activeNotificationRepository">Active Notification Repository instance.</param>
        public ActiveNotificationCreator(
            NotificationRepository notificationRepository,
            ActiveNotificationRepository activeNotificationRepository)
        {
            this.notificationRepository = notificationRepository;
            this.activeNotificationRepository = activeNotificationRepository;
        }

        /// <summary>
        /// Generate an adaptive card in json.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task CreateAsync(string notificationId)
        {
            var notification = await this.notificationRepository.GetAsync(PartitionKeyNames.Notification.DraftNotifications, notificationId);
            if (notification == null)
            {
                return;
            }

            var activeNotification = new ActiveNotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.DraftNotifications,
                RowKey = notification.Id,
                NotificationId = notification.Id,
                Content = AdaptiveCardTemplate,
                TokenExpiration = DateTime.UtcNow - TimeSpan.FromDays(1),
            };

            await this.activeNotificationRepository.CreateOrUpdateAsync(activeNotification);
        }
    }
}
