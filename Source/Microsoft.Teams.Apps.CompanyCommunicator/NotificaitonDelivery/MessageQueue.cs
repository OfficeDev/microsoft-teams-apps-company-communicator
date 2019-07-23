// <copyright file="MessageQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User;

    /// <summary>
    /// Message queue class.
    /// </summary>
    public class MessageQueue
    {
        private readonly Queue<MessageDTO> queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueue"/> class.
        /// </summary>
        public MessageQueue()
        {
            this.queue = new Queue<MessageDTO>();
        }

        /// <summary>
        /// Enqueue a delivery request item.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="userDataEntity">User Data entity.</param>
        public void Enqueue(string notificationId, UserDataEntity userDataEntity)
        {
            var message = new MessageDTO
            {
                NotificationId = notificationId,
                UserDataEntity = userDataEntity,
            };

            this.queue.Enqueue(message);
        }

        /// <summary>
        /// Enqueue a delivery request item.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="userDataEntities">User Data entities.</param>
        public void Enqueue(string notificationId, IEnumerable<UserDataEntity> userDataEntities)
        {
            foreach (var user in userDataEntities)
            {
                this.Enqueue(notificationId, user);
            }
        }

        /// <summary>
        /// Dequeue a delivery request item.
        /// </summary>
        /// <returns>MessageDTO instance.</returns>
        public MessageDTO Dequeue()
        {
            return this.queue.Dequeue();
        }
    }
}
