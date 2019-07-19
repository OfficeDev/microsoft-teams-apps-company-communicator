// <copyright file="NotificationDelivery.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.ActiveNotification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User;

    /// <summary>
    /// Notification delivery service.
    /// </summary>
    public class NotificationDelivery
    {
        private readonly ActiveNotificationRepository activeNotificationRepository;
        private readonly NotificationRepository notificationRepository;
        private readonly UserDataRepository userDataRepository;
        private readonly TeamsDataRepository teamsDataRepository;
        private readonly AdaptiveCardGenerator adaptiveCardGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDelivery"/> class.
        /// </summary>
        /// <param name="activeNotificationRepository">Active Notification Repository instance.</param>
        /// <param name="notificationRepository">Notification Repository instance.</param>
        /// <param name="teamsDataRepository">Teams Data Repository instance.</param>
        /// <param name="userDataRepository">User Data Repository instance.</param>
        /// <param name="adaptiveCardGenerator">Adaptive Card Generator instance.</param>
        public NotificationDelivery(
            ActiveNotificationRepository activeNotificationRepository,
            NotificationRepository notificationRepository,
            UserDataRepository userDataRepository,
            TeamsDataRepository teamsDataRepository,
            AdaptiveCardGenerator adaptiveCardGenerator)
        {
            this.activeNotificationRepository = activeNotificationRepository;
            this.notificationRepository = notificationRepository;
            this.userDataRepository = userDataRepository;
            this.teamsDataRepository = teamsDataRepository;
            this.adaptiveCardGenerator = adaptiveCardGenerator;
        }

        /// <summary>
        /// Send a notification to target users.
        /// </summary>
        /// <param name="notificationId">Id of the notification to be sent.</param>
        /// <returns>Indicating whether the notification was sent successfully or not.</returns>
        public bool Send(string notificationId)
        {
            var notification = this.notificationRepository.Get(PartitionKeyNames.Notification, notificationId);
            if (notification == null || !notification.IsDraft)
            {
                return false;
            }

            // Set in ActiveNotification data

            // Get all users

            // Get all teams

            // Deduplicate users

            // Set in SentNotificaiton data and counts

            // Create MB message.
            return true;
        }
    }
}
