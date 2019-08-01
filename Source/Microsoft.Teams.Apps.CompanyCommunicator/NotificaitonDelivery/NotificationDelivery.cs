// <copyright file="NotificationDelivery.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery;

    /// <summary>
    /// Notification delivery service.
    /// </summary>
    public class NotificationDelivery
    {
        private readonly UserDataProvider userDataProvider;
        private readonly ActiveNotificationCreator activeNotificationCreator;
        private readonly MessageQueue messageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDelivery"/> class.
        /// </summary>
        /// <param name="userDataProvider">User Data Provider instance.</param>
        /// <param name="activeNotificationCreator">Adaptive Card Generator instance.</param>
        /// <param name="messageQueue">Message Queue service.</param>
        public NotificationDelivery(
            UserDataProvider userDataProvider,
            ActiveNotificationCreator activeNotificationCreator,
            MessageQueue messageQueue)
        {
            this.userDataProvider = userDataProvider;
            this.activeNotificationCreator = activeNotificationCreator;
            this.messageQueue = messageQueue;
        }

        /// <summary>
        /// Send a notification to target users.
        /// </summary>
        /// <param name="draftNotificationEntity">The draft notification to be sent.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SendAsync(NotificationEntity draftNotificationEntity)
        {
            if (draftNotificationEntity == null || !draftNotificationEntity.IsDraft)
            {
                return;
            }

            // Set in ActiveNotification data
            await this.activeNotificationCreator.CreateAsync(draftNotificationEntity.Id);

            // Get all users
            var userDataDictionary = await this.userDataProvider.GetUserDataDictionaryAsync();

            // Get all teams
            var roster = await this.userDataProvider.GetAllTeamsRosterAsync();

            // Deduplicate users
            var deDuplicatedRoster = this.userDataProvider.Deduplicate(userDataDictionary, roster);

            // todo: Set in SentNotificaiton data and counts

            // Create MB message.
            this.messageQueue.Enqueue(draftNotificationEntity.Id, deDuplicatedRoster);
        }
    }
}
