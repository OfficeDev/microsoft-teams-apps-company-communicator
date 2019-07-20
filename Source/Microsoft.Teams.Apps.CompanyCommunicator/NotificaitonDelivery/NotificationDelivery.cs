// <copyright file="NotificationDelivery.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification;

    /// <summary>
    /// Notification delivery service.
    /// </summary>
    public class NotificationDelivery
    {
        private readonly NotificationRepository notificationRepository;
        private readonly UserDataProvider userDataProvider;
        private readonly ActiveNotificationCreator activeNotificationCreator;
        private readonly MessageQueue messageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDelivery"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification repository service.</param>
        /// <param name="userDataProvider">User Data Provider instance.</param>
        /// <param name="activeNotificationCreator">Adaptive Card Generator instance.</param>
        /// <param name="messageQueue">Message Queue service.</param>
        public NotificationDelivery(
            NotificationRepository notificationRepository,
            UserDataProvider userDataProvider,
            ActiveNotificationCreator activeNotificationCreator,
            MessageQueue messageQueue)
        {
            this.notificationRepository = notificationRepository;
            this.userDataProvider = userDataProvider;
            this.activeNotificationCreator = activeNotificationCreator;
            this.messageQueue = messageQueue;
        }

        /// <summary>
        /// Send a notification to target users.
        /// </summary>
        /// <param name="notificationId">Id of the notification to be sent.</param>
        /// <returns>Indicating whether the notification was sent successfully or not.</returns>
        public async Task<bool> Send(string notificationId)
        {
            var notification = this.notificationRepository.Get(PartitionKeyNames.Notification, notificationId);
            if (notification == null || !notification.IsDraft)
            {
                return false;
            }

            // Set in ActiveNotification data
            this.activeNotificationCreator.Create(notificationId);

            // Get all users
            var userDataDictionary = this.userDataProvider.GetUserDataDictionary();

            // Get all teams
            var roster = await this.userDataProvider.GetAllTeamsRosterAsync();

            // Deduplicate users
            var deDuplicatedRoster = this.userDataProvider.Deduplicate(userDataDictionary, roster);

            // todo: Set in SentNotificaiton data and counts

            // Create MB message.
            this.messageQueue.Enqueue(notificationId, deDuplicatedRoster);

            return true;
        }
    }
}
