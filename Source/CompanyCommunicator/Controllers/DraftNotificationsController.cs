﻿// <copyright file="DraftNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

using System.IO;
using System.Net.Mime;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
using Microsoft.Teams.Apps.CompanyCommunicator.Controllers.Options;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.DraftNotificationPreview;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions;
    using Newtonsoft.Json;

    /// <summary>
    /// Controller for the draft notification data.
    /// </summary>
    [Route("api/draftNotifications")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class DraftNotificationsController : ControllerBase
    {
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ITeamDataRepository teamDataRepository;
        private readonly IDraftNotificationPreviewService draftNotificationPreviewService;
        private readonly IGroupsService groupsService;
        private readonly IStorageClientFactory storageClientFactory;
        private readonly UserAppOptions userAppOptions;
        private readonly IAppSettingsService appSettingsService;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository instance.</param>
        /// <param name="teamDataRepository">Team data repository instance.</param>
        /// <param name="draftNotificationPreviewService">Draft notification preview service.</param>
        /// <param name="appSettingsService">App Settings service.</param>
        /// <param name="localizer">Localization service.</param>
        /// <param name="groupsService">group service.</param>
        /// <param name="storageClientFactory">Storage Library</param>
        public DraftNotificationsController(
            INotificationDataRepository notificationDataRepository,
            ITeamDataRepository teamDataRepository,
            IDraftNotificationPreviewService draftNotificationPreviewService,
            IAppSettingsService appSettingsService,
            IStringLocalizer<Strings> localizer,
            IGroupsService groupsService,
            IStorageClientFactory storageClientFactory,
            IOptions<UserAppOptions> userAppOptions)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.teamDataRepository = teamDataRepository ?? throw new ArgumentNullException(nameof(teamDataRepository));
            this.draftNotificationPreviewService = draftNotificationPreviewService ?? throw new ArgumentNullException(nameof(draftNotificationPreviewService));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            this.groupsService = groupsService ?? throw new ArgumentNullException(nameof(groupsService));
            this.storageClientFactory = storageClientFactory ?? throw new ArgumentNullException(nameof(storageClientFactory));
            this.userAppOptions = userAppOptions?.Value ?? throw new ArgumentNullException(nameof(userAppOptions));
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        }

        /// <summary>
        /// Create a new draft notification.
        /// </summary>
        /// <param name="notification">A new Draft Notification to be created.</param>
        /// <returns>The created notification's id.</returns>
        [HttpPost]
        public async Task<ActionResult<string>> CreateDraftNotificationAsync([FromBody] DraftNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (!notification.Validate(this.localizer, out string errorMessage, this.userAppOptions.MaxNumberOfTeams))
            {
                return this.BadRequest(errorMessage);
            }

            var containsHiddenMembership = await this.groupsService.ContainsHiddenMembershipAsync(notification.Groups);
            if (containsHiddenMembership)
            {
                return this.Forbid();
            }

            if (!this.CheckUrl(notification.ImageLink))
            {
                await this.UploadToBlobStorage(notification);
            }

            if (!this.userAppOptions.DisableReadTracking)
            {
                notification.TrackingUrl = this.HttpContext.Request.Scheme + "://" + this.HttpContext.Request.Host + "/api/sentNotifications/tracking";
            }

            var notificationId = await this.notificationDataRepository.CreateDraftNotificationAsync(
                notification,
                this.HttpContext.User?.Identity?.Name);
            return this.Ok(notificationId);
        }

        private bool CheckUrl(string urlString)
        {
            Uri uriResult;

            if (Uri.TryCreate(urlString, UriKind.Absolute, out uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttps;
            }
            return false;
        }

        private async Task UploadToBlobStorage(DraftNotification notification)
        {
            if (this.userAppOptions.ImageUploadBlobStorage && !string.IsNullOrWhiteSpace(notification.ImageLink))
            {
                var offset = notification.ImageLink.IndexOf(',') + 1;
                var imageBytes = Convert.FromBase64String(notification.ImageLink[offset..^0]);

                await using var stream = new MemoryStream(imageBytes, writable: false);
                var blobContainerClient = this.storageClientFactory.CreateBlobContainerClient("imageupload");
                await blobContainerClient.CreateIfNotExistsAsync();

                var blob = blobContainerClient.GetBlobClient(Guid.NewGuid().ToString() + ".jpg");
                await blob.UploadAsync(stream, true);

                if (blobContainerClient.CanGenerateSasUri)
                {
                    // Create a SAS token that's valid for one hour.
                    BlobSasBuilder sasBuilder = new BlobSasBuilder()
                    {
                        BlobContainerName = blobContainerClient.Name,
                        BlobName = blob.Name,
                        Resource = "b"
                    };

                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(this.userAppOptions.ImageUploadBlobStorageSasDurationHours);
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    notification.ImageLink = blob.GenerateSasUri(sasBuilder).AbsoluteUri;
                }
            }
        }

        /// <summary>
        /// Duplicate an existing draft notification.
        /// </summary>
        /// <param name="id">The id of a Draft Notification to be duplicated.</param>
        /// <returns>If the passed in id is invalid, it returns 404 not found error. Otherwise, it returns 200 OK.</returns>
        [HttpPost("duplicates/{id}")]
        public async Task<IActionResult> DuplicateDraftNotificationAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var notificationEntity = await this.FindNotificationToDuplicate(id);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var createdBy = this.HttpContext.User?.Identity?.Name;
            notificationEntity.Title = this.localizer.GetString("DuplicateText", notificationEntity.Title);
            await this.notificationDataRepository.DuplicateDraftNotificationAsync(notificationEntity, createdBy);

            return this.Ok();
        }

        /// <summary>
        /// Update an existing draft notification.
        /// </summary>
        /// <param name="notification">An existing Draft Notification to be updated.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateDraftNotificationAsync([FromBody] DraftNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var containsHiddenMembership = await this.groupsService.ContainsHiddenMembershipAsync(notification.Groups);
            if (containsHiddenMembership)
            {
                return this.Forbid();
            }

            if (!notification.Validate(this.localizer, out string errorMessage, this.userAppOptions.MaxNumberOfTeams))
            {
                return this.BadRequest(errorMessage);
            }

            if (!string.IsNullOrWhiteSpace(notification.ImageLink) && notification.ImageLink.StartsWith("data:image/"))
            {
                await this.UploadToBlobStorage(notification);
            }


            // TODO: double-check it
           // notification.Buttons = this.GetButtonTrackingUrl(notification);


            var notificationEntity = new NotificationDataEntity
            {
                PartitionKey = NotificationDataTableNames.DraftNotificationsPartition,
                RowKey = notification.Id,
                Id = notification.Id,
                Title = notification.Title,
                ImageLink = notification.ImageLink,
                Summary = notification.Summary,
                Author = notification.Author,
                ButtonTitle = notification.ButtonTitle,
                ButtonLink = notification.ButtonLink,
                ChannelId = notification.ChannelId,
                ChannelImage = notification.ChannelImage,
                ChannelTitle = notification.ChannelTitle,
                CreatedBy = this.HttpContext.User?.Identity?.Name,
                CreatedDate = DateTime.UtcNow,
                IsDraft = true,
                IsScheduled = notification.IsScheduled,
                IsImportant = notification.IsImportant,
                ScheduledDate = notification.ScheduledDate,
                Teams = notification.Teams,
                Rosters = notification.Rosters,
                Groups = notification.Groups,
                CsvUsers = notification.CsvUsers,
                AllUsers = notification.AllUsers,
                Buttons = notification.Buttons,
                TrackingUrl = this.HttpContext.Request.Scheme + "://" + this.HttpContext.Request.Host + "/api/sentNotifications/tracking",
            };

            await this.notificationDataRepository.CreateOrUpdateAsync(notificationEntity);
            return this.Ok();
        }

        /// <summary>
        /// Delete an existing draft notification.
        /// </summary>
        /// <param name="id">The id of the draft notification to be deleted.</param>
        /// <returns>If the passed in Id is invalid, it returns 404 not found error. Otherwise, it returns 200 OK.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDraftNotificationAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                id);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            await this.notificationDataRepository.DeleteAsync(notificationEntity);
            return this.Ok();
        }

        /// <summary>
        /// Get draft notifications.
        /// </summary>
        /// <returns>A list of <see cref="DraftNotificationSummary"/> instances.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DraftNotificationSummary>>> GetAllDraftNotificationsAsync()
        {
            var notificationEntities = await this.notificationDataRepository.GetAllDraftNotificationsAsync();

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
        /// Get scheduled notifications. Those are draft notifications with a scheduledate.
        /// </summary>
        /// <returns>A list of <see cref="DraftNotificationSummary"/> instances.</returns>
        [HttpGet("scheduled")]
        public async Task<ActionResult<IEnumerable<DraftNotificationSummary>>> GetAllScheduledNotificationsAsync()
        {
            var notificationEntities = await this.notificationDataRepository.GetAllScheduledNotificationsAsync();

            var result = new List<DraftNotificationSummary>();
            foreach (var notificationEntity in notificationEntities)
            {
                var summary = new DraftNotificationSummary
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                    ScheduledDate = notificationEntity.ScheduledDate,
                };

                result.Add(summary);
            }

            // sorts the scheduled messages by date from the most recent
            result.Sort((r1, r2) => r1.ScheduledDate.Value.CompareTo(r2.ScheduledDate.Value));
            return result;
        }

        /// <summary>
        /// Get scheduled notifications. Those are draft notifications with a scheduledate.
        /// </summary>
        /// <param name="channelId">Channel ID to filter scheduled notifications.</param>
        /// <returns>A list of <see cref="DraftNotificationSummary"/> instances.</returns>
        [HttpGet("scheduled/channel/{channelId}")]
        public async Task<ActionResult<IEnumerable<DraftNotificationSummary>>> GetChannelScheduledNotificationsAsync(string channelId)
        {
            var notificationEntities = await this.notificationDataRepository.GetChannelScheduledNotificationsAsync(channelId);

            var result = new List<DraftNotificationSummary>();
            foreach (var notificationEntity in notificationEntities)
            {
                var summary = new DraftNotificationSummary
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                    ScheduledDate = notificationEntity.ScheduledDate,
                };

                result.Add(summary);
            }

            // sorts the scheduled messages by date from the most recent
            result.Sort((r1, r2) => r1.ScheduledDate.Value.CompareTo(r2.ScheduledDate.Value));
            return result;
        }

        /// <summary>
        /// Get draft notifications filtered by channel.
        /// </summary>
        /// <param name="channelId">Channel Id.</param>
        /// <returns>A list of <see cref="DraftNotificationSummary"/> instances.</returns>
        [HttpGet("channel/{channelId}")]
        public async Task<ActionResult<IEnumerable<DraftNotificationSummary>>> GetChannelDraftNotifications(string channelId)
        {
            var notificationEntities = await this.notificationDataRepository.GetChannelDraftNotificationsAsync(channelId);

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
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                id);
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
                CreatedDateTime = notificationEntity.CreatedDate,
                Teams = notificationEntity.Teams,
                Rosters = notificationEntity.Rosters,
                Groups = notificationEntity.Groups,
                CsvUsers = notificationEntity.CsvUsers,
                AllUsers = notificationEntity.AllUsers,
                IsScheduled = notificationEntity.IsScheduled,
                IsImportant = notificationEntity.IsImportant,
                ScheduledDate = notificationEntity.ScheduledDate,
                Buttons = notificationEntity.Buttons,
                TrackingUrl = notificationEntity.TrackingUrl,
                ChannelId = notificationEntity.ChannelId,
                ChannelTitle = notificationEntity.ChannelTitle,
                ChannelImage = notificationEntity.ChannelImage,
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
            if (notificationId == null)
            {
                throw new ArgumentNullException(nameof(notificationId));
            }

            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                notificationId);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var groupNames = await this.groupsService
                .GetByIdsAsync(notificationEntity.Groups)
                .Select(x => x.DisplayName).
                ToListAsync();

            var result = new DraftNotificationSummaryForConsent
            {
                NotificationId = notificationId,
                TeamNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Teams),
                RosterNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Rosters),
                GroupNames = groupNames,
                AllUsers = notificationEntity.AllUsers,
            };

            return this.Ok(result);
        }

        /// <summary>
        /// Preview draft notification.
        /// </summary>
        /// <param name="draftNotificationPreviewRequest">Draft notification preview request.</param>
        /// <returns>
        /// It returns 400 bad request error if the incoming parameter, draftNotificationPreviewRequest, is invalid.
        /// It returns 404 not found error if the DraftNotificationId or TeamsTeamId (contained in draftNotificationPreviewRequest) is not found in the table storage.
        /// It returns 500 internal error if this method throws an unhandled exception.
        /// It returns 429 too many requests error if the preview request is throttled by the bot service.
        /// It returns 200 OK if the method is executed successfully.</returns>
        [HttpPost("previews")]
        public async Task<ActionResult> PreviewDraftNotificationAsync(
            [FromBody] DraftNotificationPreviewRequest draftNotificationPreviewRequest)
        {
            if (draftNotificationPreviewRequest == null
                || string.IsNullOrWhiteSpace(draftNotificationPreviewRequest.DraftNotificationId)
                || string.IsNullOrWhiteSpace(draftNotificationPreviewRequest.TeamsTeamId)
                || string.IsNullOrWhiteSpace(draftNotificationPreviewRequest.TeamsChannelId))
            {
                return this.BadRequest();
            }

            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                draftNotificationPreviewRequest.DraftNotificationId);
            if (notificationEntity == null)
            {
                return this.BadRequest($"Notification {draftNotificationPreviewRequest.DraftNotificationId} not found.");
            }

            var teamDataEntity = new TeamDataEntity();
            teamDataEntity.TenantId = this.HttpContext.User.FindFirstValue(Common.Constants.ClaimTypeTenantId);
            teamDataEntity.ServiceUrl = await this.appSettingsService.GetServiceUrlAsync();
            var result = await this.draftNotificationPreviewService.SendPreview(
                notificationEntity,
                teamDataEntity,
                draftNotificationPreviewRequest.TeamsChannelId);
            return this.StatusCode((int)result);
        }

        private async Task<NotificationDataEntity> FindNotificationToDuplicate(string notificationId)
        {
            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                notificationId);
            if (notificationEntity == null)
            {
                notificationEntity = await this.notificationDataRepository.GetAsync(
                    NotificationDataTableNames.SentNotificationsPartition,
                    notificationId);
            }

            return notificationEntity;
        }
    }
}
