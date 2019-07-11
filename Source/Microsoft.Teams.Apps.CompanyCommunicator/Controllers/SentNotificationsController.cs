// <copyright file="SentNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;

    /// <summary>
    /// Controller for the sent notification data.
    /// </summary>
    public class SentNotificationsController
    {
        private readonly INotificationRepository notificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification respository service that deals with the table storage in azure.</param>
        public SentNotificationsController(INotificationRepository notificationRepository)
        {
            this.notificationRepository = notificationRepository;
        }

        /// <summary>
        /// Create a new sent notification.
        /// </summary>
        /// <param name="notification">An instance of <see cref="Notification"/> class.</param>
        [HttpPost("api/sentNotifications")]
        public void CreateSentNotification([FromBody]Notification notification)
        {
            var notificationEntity = new NotificationEntity
            {
                PartitionKey = "Announcement",
                RowKey = notification.Id,
                Title = notification.Title,
                IsDraft = false,
            };

            this.notificationRepository.CreateOrUpdate(notificationEntity);
        }

        /// <summary>
        /// Get sent notifications.
        /// </summary>
        /// <returns>A list of <see cref="Notification"/> instances.</returns>
        [HttpGet("api/sentNotifications")]
        public IEnumerable<Notification> GetSentNotifications()
        {
            var notificationEntities = this.notificationRepository.All(false);

            var result = new List<Notification>();
            foreach (var notificationEntity in notificationEntities)
            {
                var notification = new Notification
                {
                    Id = notificationEntity.RowKey,
                    Title = notificationEntity.Title,
                    Date = notificationEntity.Date,
                    Recipients = "30,0,1",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                };

                result.Add(notification);
            }

            return result;
        }

        /// <summary>
        /// Get a sent notification by Id.
        /// </summary>
        /// <param name="id">Id of the requested sent notification.</param>
        /// <returns>Required sent notification.</returns>
        [HttpGet("api/sentNotifications/{id}")]
        public Notification GetSentNotificationById(string id)
        {
            var notificationEntity = this.notificationRepository.Get("Announcement", id);
            if (notificationEntity == null)
            {
                return null;
            }

            var result = new Notification
            {
                Id = id,
                Title = notificationEntity.Title,
                Date = notificationEntity.Date,
                Recipients = "30,0,1",
                Acknowledgements = "acknowledgements",
                Reactions = "like 3",
                Responses = "view 3",
            };

            return result;
        }
    }
}
