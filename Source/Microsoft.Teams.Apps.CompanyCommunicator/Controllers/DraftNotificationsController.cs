// <copyright file="DraftNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions;

    /// <summary>
    /// Controller for the draft notification data.
    /// </summary>
    [Route("api/draftNotifications")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class DraftNotificationsController : ControllerBase
    {
        private readonly NotificationRepository notificationRepository;
        private readonly TeamDataRepository teamDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification respository instance.</param>
        /// <param name="teamDataRepository">Team data repository instance.</param>
        public DraftNotificationsController(
            NotificationRepository notificationRepository,
            TeamDataRepository teamDataRepository)
        {
            this.notificationRepository = notificationRepository;
            this.teamDataRepository = teamDataRepository;
        }

        /// <summary>
        /// Create a new draft notification.
        /// </summary>
        /// <param name="notification">A new Draft Notification to be created.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [HttpPost]
        public async Task CreateDraftNotificationAsync([FromBody]DraftNotification notification)
        {
            await this.notificationRepository.CreateDraftNotificationAsync(
                notification,
                this.HttpContext.User?.Identity?.Name);
        }

        /// <summary>
        /// Duplicate an existing draft notification.
        /// </summary>
        /// <param name="id">The id of a Draft Notification to be duplicated.</param>
        /// <returns>If the passed in id is invalid, it returns 404 not found error. Otherwise, it returns 200 Ok.</returns>
        [HttpPost("duplicates/{id}")]
        public async Task<IActionResult> DuplicateDraftNotificationAsync(string id)
        {
            var notificationEntity = await this.notificationRepository.GetAsync(PartitionKeyNames.Notification.DraftNotifications, id);

            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var newId = Guid.NewGuid().ToString();
            var newNotificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.DraftNotifications,
                RowKey = newId,
                Id = newId,
                Title = notificationEntity.Title,
                ImageLink = notificationEntity.ImageLink,
                Summary = notificationEntity.Summary,
                Author = notificationEntity.Author,
                ButtonTitle = notificationEntity.ButtonTitle,
                ButtonLink = notificationEntity.ButtonLink,
                CreatedBy = this.HttpContext.User?.Identity?.Name,
                CreatedDate = DateTime.UtcNow.ToShortDateString(),
                IsDraft = true,
                Teams = notificationEntity.Teams,
                Rosters = notificationEntity.Rosters,
                AllUsers = notificationEntity.AllUsers,
            };

            await this.notificationRepository.CreateOrUpdateAsync(newNotificationEntity);

            return this.Ok();
        }

        /// <summary>
        /// Update an existing draft notification.
        /// </summary>
        /// <param name="notification">An existing Draft Notification to be updated.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [HttpPut]
        public async Task UpdateDraftNotificationAsync([FromBody]DraftNotification notification)
        {
            var notificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.DraftNotifications,
                RowKey = notification.Id,
                Id = notification.Id,
                Title = notification.Title,
                ImageLink = notification.ImageLink,
                Summary = notification.Summary,
                Author = notification.Author,
                ButtonTitle = notification.ButtonTitle,
                ButtonLink = notification.ButtonLink,
                CreatedBy = this.HttpContext.User?.Identity?.Name,
                CreatedDate = DateTime.UtcNow.ToShortDateString(),
                IsDraft = true,
                Teams = notification.Teams,
                Rosters = notification.Rosters,
                AllUsers = notification.AllUsers,
            };

            await this.notificationRepository.CreateOrUpdateAsync(notificationEntity);
        }

        /// <summary>
        /// Delete an existing draft notification.
        /// </summary>
        /// <param name="id">The id of the draft notification to be deleted.</param>
        /// <returns>If the passed in Id is invalid, it returns 404 not found error. Otherwise, it returns 200 Ok.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDraftNotificationAsync(string id)
        {
            var notificationEntity = await this.notificationRepository.GetAsync(PartitionKeyNames.Notification.DraftNotifications, id);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            await this.notificationRepository.DeleteAsync(notificationEntity);
            return this.Ok();
        }

        /// <summary>
        /// Get draft notifications.
        /// </summary>
        /// <returns>A list of <see cref="DraftNotificationSummary"/> instances.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DraftNotificationSummary>>> GetAllDraftNotificationsAsync()
        {
            var notificationEntities = await this.notificationRepository.GetAllAsync(true);

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
        /// <returns>It returns the draft notification with the passed in id.
        /// The returning value is wrapped in a ActionResult object.
        /// If the passed in id is invalid, it returns 404 not found error.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<DraftNotification>> GetDraftNotificationByIdAsync(string id)
        {
            var notificationEntity = await this.notificationRepository.GetAsync(PartitionKeyNames.Notification.DraftNotifications, id);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

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
                Teams = notificationEntity.Teams,
                Rosters = notificationEntity.Rosters,
                AllUsers = notificationEntity.AllUsers,
            };

            return this.Ok(result);
        }

        /// <summary>
        /// Get draft notification summary (for consent page) by notification Id.
        /// </summary>
        /// <param name="notificationId">Draft notification Id.</param>
        /// <returns>It returns the draft notification summary (for consent page) with the passed in id.
        /// If the passed in id is invalid, it returns 404 not found error.</returns>
        [HttpGet("consentSummaries/{notificationId}")]
        public async Task<ActionResult<DraftNotificationSummaryForConsent>> GetDraftNotificationSummaryForConsentByIdAsync(string notificationId)
        {
            var notificationEntity = await this.notificationRepository.GetAsync(PartitionKeyNames.Notification.DraftNotifications, notificationId);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var result = new DraftNotificationSummaryForConsent
            {
                NotificationId = notificationId,
                TeamNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Teams),
                RosterNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Rosters),
                AllUsers = notificationEntity.AllUsers,
            };

            return this.Ok(result);
        }

        /// <summary>
        /// Get draft notifification summary (for consent page) by notification Id.
        /// </summary>
        /// <param name="notificationId">Draft notification Id.</param>
        /// <returns>It returns the draft notification summary (for consent page) with the passed in id.
        /// If the passed in id is invalid, it returns 404 not found error.</returns>
        [HttpGet("consentSummaries/{notificationId}")]
        public async Task<ActionResult<DraftNotificationSummaryForConsent>> GetDraftNotificationSummaryForConsentByIdAsync(string notificationId)
        {
            var notificationEntity = await this.notificationRepository.GetAsync(PartitionKeyNames.Notification.DraftNotifications, notificationId);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var result = new DraftNotificationSummaryForConsent
            {
                NotificationId = notificationId,
                TeamNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Teams),
                RosterNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Rosters),
                AllUsers = notificationEntity.AllUsers,
            };

            return this.Ok(result);
        }
    }
}