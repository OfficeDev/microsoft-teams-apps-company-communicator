// <copyright file="ActiveNotificationCreator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.ActiveNotification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification;

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
        public void Create(string notificationId)
        {
            var notification = this.notificationRepository.Get(PartitionKeyNames.Notification, notificationId);
            if (notification == null)
            {
                return;
            }

            var activeNotification = new ActiveNotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification,
                RowKey = notification.Id,
                NotificationId = notification.Id,
                Content = AdaptiveCardTemplate,
                TokenExpiration = DateTime.UtcNow - TimeSpan.FromDays(1),
            };

            this.activeNotificationRepository.CreateOrUpdate(activeNotification);
        }
    }
}
