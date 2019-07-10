// <copyright file="DraftNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;

    /// <summary>
    /// Controller for the draft notification data.
    /// </summary>
    public class DraftNotificationsController
    {
        private readonly INotificationRepository notificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification respository service that deals with the table storage in azure.</param>
        public DraftNotificationsController(INotificationRepository notificationRepository)
        {
            this.notificationRepository = notificationRepository;
        }

        /// <summary>
        /// Create a new draft notification.
        /// </summary>
        /// <param name="notification">An instance of <see cref="Notification"/> class.</param>
        [HttpPost("api/draftNotifications")]
        public void CreateDraftNotification([FromBody]Notification notification)
        {
            var notificationEntity = new NotificationEntity
            {
                PartitionKey = "Notification",
                RowKey = notification.Id,
                Title = notification.Title,
                IsDraft = true,
            };

            this.notificationRepository.CreateOrUpdate(notificationEntity);
        }

        /// <summary>
        /// Get draft notifications.
        /// </summary>
        /// <returns>A list of <see cref="Notification"/> instances.</returns>
        [HttpGet("api/draftNotifications")]
        public IEnumerable<Notification> GetDraftNotifications()
        {
            var notificationEntities = this.notificationRepository.All(true);

            var result = new List<Notification>();
            foreach (var notificationEntity in notificationEntities)
            {
                var notification = new Notification
                {
                    Id = notificationEntity.RowKey,
                    Title = notificationEntity.RowKey,
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
        /// Get a draft notification by Id.
        /// </summary>
        /// <param name="id">Draft notification Id.</param>
        /// <returns>Required draft notification.</returns>
        [HttpGet("api/draftNotifications/{id}")]
        public Notification GetDraftNotificationById(string id)
        {
            var notificationEntity = this.notificationRepository.Get("Notification", id);

            var result = new Notification
            {
                Id = id,
                Title = notificationEntity.RowKey,
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
