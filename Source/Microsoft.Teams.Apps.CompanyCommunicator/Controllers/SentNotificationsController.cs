// <copyright file="SentNotificationsController.cs" company="Microsoft">
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
    /// Controller for the sent notification data.
    /// </summary>
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class SentNotificationsController : ControllerBase
    {
        private readonly NotificationRepository notificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification respository service that deals with the table storage in azure.</param>
        public SentNotificationsController(NotificationRepository notificationRepository)
        {
            this.notificationRepository = notificationRepository;
        }

        /// <summary>
        /// Send a draft notification, which turns the draft to be a sent notification.
        /// </summary>
        /// <param name="notification">An instance of <see cref="DraftNotification"/> class.</param>
        /// <returns>The result of an action method.</returns>
        [HttpPost("api/sentNotifications")]
        public IActionResult CreateSentNotification([FromBody]DraftNotification notification)
        {
            var notificationEntity = this.notificationRepository.Get(PartitionKeyNames.Notification, notification.Id);
            if (notificationEntity != null)
            {
                notificationEntity.IsDraft = false;
                notificationEntity.SentDate = DateTime.UtcNow.ToShortDateString();
                this.notificationRepository.CreateOrUpdate(notificationEntity);

                return this.Ok();
            }

            return this.NotFound();
        }

        /// <summary>
        /// Get all sent notification summaries.
        /// </summary>
        /// <returns>A list of <see cref="SentNotificationSummary"/> instances.</returns>
        [HttpGet("api/sentNotifications")]
        public IEnumerable<SentNotificationSummary> GetSentNotifications()
        {
            var notificationEntities = this.notificationRepository.All(false);

            var result = new List<SentNotificationSummary>();
            foreach (var notificationEntity in notificationEntities)
            {
                var summary = new SentNotificationSummary
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                    CreatedDate = notificationEntity.CreatedDate,
                    SentDate = notificationEntity.SentDate,
                    Recipients = $"{notificationEntity.Succeeded},{notificationEntity.Failed},{notificationEntity.Throttled}",
                };

                result.Add(summary);
            }

            return result;
        }

        /// <summary>
        /// Get a sent notification by Id.
        /// </summary>
        /// <param name="id">Id of the requested sent notification.</param>
        /// <returns>Required sent notification.</returns>
        [HttpGet("api/sentNotifications/{id}")]
        public IActionResult GetSentNotificationById(string id)
        {
            var notificationEntity = this.notificationRepository.Get(PartitionKeyNames.Notification, id);
            if (notificationEntity != null)
            {
                var result = new SentNotification
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                    ImageLink = notificationEntity.ImageLink,
                    Summary = notificationEntity.Summary,
                    Author = notificationEntity.Author,
                    ButtonTitle = notificationEntity.ButtonTitle,
                    ButtonLink = notificationEntity.ButtonLink,
                    CreatedDate = notificationEntity.CreatedDate,
                    Succeeded = notificationEntity.Succeeded,
                    Failed = notificationEntity.Failed,
                    Throttled = notificationEntity.Throttled,
                };

                return this.Ok(result);
            }

            return this.NotFound();
        }
    }
}
