// <copyright file="DraftNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification;

    /// <summary>
    /// Controller for the draft notification data.
    /// </summary>
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class DraftNotificationsController : ControllerBase
    {
        private readonly NotificationRepository notificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification respository instance.</param>
        public DraftNotificationsController(NotificationRepository notificationRepository)
        {
            this.notificationRepository = notificationRepository;
        }

        /// <summary>
        /// Create a new draft notification.
        /// </summary>
        /// <param name="notification">An instance of <see cref="DraftNotification"/> class.</param>
        [HttpPost("api/draftNotifications")]
        public void CreateDraftNotification([FromBody]DraftNotification notification)
        {
            var id = Guid.NewGuid().ToString();
            var notificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification,
                RowKey = id,
                Id = id,
                Title = notification.Title,
                ImageLink = notification.ImageLink,
                Summary = notification.Summary,
                Author = notification.Author,
                ButtonTitle = notification.ButtonTitle,
                ButtonLink = notification.ButtonLink,
                CreatedDate = DateTime.UtcNow.ToShortDateString(),
                IsDraft = true,
                Teams = this.IdToAudience(notification.Teams),
                Rosters = this.IdToAudience(notification.Rosters),
                AllUsers = notification.AllUsers,
            };

            this.notificationRepository.CreateOrUpdate(notificationEntity);
        }

        /// <summary>
        /// Get draft notifications.
        /// </summary>
        /// <returns>A list of <see cref="DraftNotificationSummary"/> instances.</returns>
        [HttpGet("api/draftNotifications")]
        public IEnumerable<DraftNotificationSummary> GetDraftNotifications()
        {
            var notificationEntities = this.notificationRepository.All(true);

            var result = new List<DraftNotificationSummary>();
            foreach (var notificationEntity in notificationEntities)
            {
                var summary = new DraftNotificationSummary
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                };

                result.Add(summary);
            }

            return result;
        }

        /// <summary>
        /// Get a draft notification by Id.
        /// </summary>
        /// <param name="id">Draft notification Id.</param>
        /// <returns>The draft notification with the specific id.</returns>
        [HttpGet("api/draftNotifications/{id}")]
        public IActionResult GetDraftNotificationById(string id)
        {
            var notificationEntity = this.notificationRepository.Get(PartitionKeyNames.Notification, id);
            if (notificationEntity != null)
            {
                var result = new DraftNotification
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                    ImageLink = notificationEntity.ImageLink,
                    Summary = notificationEntity.Summary,
                    Author = notificationEntity.Author,
                    ButtonTitle = notificationEntity.ButtonTitle,
                    ButtonLink = notificationEntity.ButtonLink,
                    CreatedDate = notificationEntity.CreatedDate,
                    Teams = this.AudienceToId(notificationEntity.Teams),
                    Rosters = this.AudienceToId(notificationEntity.Rosters),
                    AllUsers = notificationEntity.AllUsers,
                };

                return this.Ok(result);
            }

            return this.NotFound();
        }

        private IEnumerable<AudienceEntity> IdToAudience(IEnumerable<string> teamIds)
        {
            var result = new List<AudienceEntity>();
            if (teamIds != null)
            {
                foreach (var id in teamIds)
                {
                    var audience = new AudienceEntity
                    {
                        TeamId = id,
                        DeliveryState = DeliveryStatus.Pending,
                    };
                    result.Add(audience);
                }
            }

            return result;
        }

        private IEnumerable<string> AudienceToId(IEnumerable<AudienceEntity> audiences)
        {
            var result = new List<string>();
            if (audiences != null)
            {
                foreach (var audience in audiences)
                {
                    result.Add(audience.TeamId);
                }
            }

            return result;
        }
    }
}
