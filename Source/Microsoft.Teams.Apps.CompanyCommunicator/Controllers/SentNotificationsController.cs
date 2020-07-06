// <copyright file="SentNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Controller for the sent notification data.
    /// </summary>
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    [Route("api/sentNotifications")]
    public class SentNotificationsController : ControllerBase
    {
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly TeamDataRepository teamDataRepository;
        private readonly PrepareToSendQueue prepareToSendQueue;
        private readonly DataQueue dataQueue;
        private readonly double forceCompleteMessageDelayInSeconds;
        private readonly SendBatchesDataRepository sendBatchesDataRepository;
        private readonly IGroupsService groupsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationsController"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository service that deals with the table storage in azure.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="teamDataRepository">Team data repository instance.</param>
        /// <param name="prepareToSendQueue">The service bus queue for preparing to send notifications.</param>
        /// <param name="dataQueue">The service bus queue for the data queue.</param>
        /// <param name="dataQueueMessageOptions">The options for the data queue messages.</param>
        /// <param name="sendBatchesDataRepository">The send batches data repository.</param>
        /// <param name="groupsService">graph service.</param>
        public SentNotificationsController(
            NotificationDataRepository notificationDataRepository,
            SentNotificationDataRepository sentNotificationDataRepository,
            TeamDataRepository teamDataRepository,
            PrepareToSendQueue prepareToSendQueue,
            DataQueue dataQueue,
            IOptions<DataQueueMessageOptions> dataQueueMessageOptions,
            SendBatchesDataRepository sendBatchesDataRepository,
            IGroupsService groupsService)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.teamDataRepository = teamDataRepository;
            this.prepareToSendQueue = prepareToSendQueue;
            this.dataQueue = dataQueue;
            this.forceCompleteMessageDelayInSeconds = dataQueueMessageOptions.Value.ForceCompleteMessageDelayInSeconds;
            this.sendBatchesDataRepository = sendBatchesDataRepository;
            this.groupsService = groupsService;
        }

        /// <summary>
        /// Send a notification, which turns a draft to be a sent notification.
        /// </summary>
        /// <param name="draftNotification">An instance of <see cref="DraftNotification"/> class.</param>
        /// <returns>The result of an action method.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateSentNotificationAsync(
            [FromBody]DraftNotification draftNotification)
        {
            var draftNotificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                draftNotification.Id);
            if (draftNotificationDataEntity == null)
            {
                return this.NotFound($"Draft notification, Id: {draftNotification.Id}, could not be found.");
            }

            var newSentNotificationId =
                await this.notificationDataRepository.MoveDraftToSentPartitionAsync(draftNotificationDataEntity);

            // Ensure the data tables needed by the Azure Functions to send the notifications exist in Azure storage.
            await Task.WhenAll(
                this.sentNotificationDataRepository.EnsureSentNotificationDataTableExistsAsync(),
                this.sendBatchesDataRepository.EnsureSendBatchesDataTableExistsAsync());

            var prepareToSendQueueMessageContent = new PrepareToSendQueueMessageContent
            {
                NotificationId = newSentNotificationId,
            };
            await this.prepareToSendQueue.SendAsync(prepareToSendQueueMessageContent);

            // Send a "force complete" message to the data queue with a delay to ensure that
            // the notification will be marked as complete no matter the counts
            var forceCompleteDataQueueMessageContent = new DataQueueMessageContent
            {
                NotificationId = newSentNotificationId,
                ForceMessageComplete = true,
            };
            await this.dataQueue.SendDelayedAsync(
                forceCompleteDataQueueMessageContent,
                this.forceCompleteMessageDelayInSeconds);

            return this.Ok();
        }

        /// <summary>
        /// Get most recently sent notification summaries.
        /// </summary>
        /// <returns>A list of <see cref="SentNotificationSummary"/> instances.</returns>
        [HttpGet]
        public async Task<IEnumerable<SentNotificationSummary>> GetSentNotificationsAsync()
        {
            var notificationEntities = await this.notificationDataRepository.GetMostRecentSentNotificationsAsync();

            var result = new List<SentNotificationSummary>();
            foreach (var notificationEntity in notificationEntities)
            {
                var summary = new SentNotificationSummary
                {
                    Id = notificationEntity.Id,
                    Title = notificationEntity.Title,
                    CreatedDateTime = notificationEntity.CreatedDate,
                    SentDate = notificationEntity.SentDate,
                    Succeeded = notificationEntity.Succeeded,
                    Failed = notificationEntity.Failed,
                    Throttled = notificationEntity.Throttled,
                    TotalMessageCount = notificationEntity.TotalMessageCount,
                    IsCompleted = notificationEntity.IsCompleted,
                    SendingStartedDate = notificationEntity.SendingStartedDate,
                    IsPreparingToSend = notificationEntity.IsPreparingToSend,
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSentNotificationByIdAsync(string id)
        {
            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                id);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var groupNames = new List<string>();
            var groupList = notificationEntity.Groups.ToList();
            await foreach (var group in
                this.groupsService.GetByIdsAsync(groupList))
            {
                groupNames.Add(group.DisplayName);
            }

            var result = new SentNotification
            {
                Id = notificationEntity.Id,
                Title = notificationEntity.Title,
                ImageLink = notificationEntity.ImageLink,
                Summary = notificationEntity.Summary,
                Author = notificationEntity.Author,
                ButtonTitle = notificationEntity.ButtonTitle,
                ButtonLink = notificationEntity.ButtonLink,
                CreatedDateTime = notificationEntity.CreatedDate,
                SentDate = notificationEntity.SentDate,
                Succeeded = notificationEntity.Succeeded,
                Failed = notificationEntity.Failed,
                Throttled = notificationEntity.Throttled,
                TeamNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Teams),
                RosterNames = await this.teamDataRepository.GetTeamNamesByIdsAsync(notificationEntity.Rosters),
                GroupNames = groupNames,
                AllUsers = notificationEntity.AllUsers,
                SendingStartedDate = notificationEntity.SendingStartedDate,
                ErrorMessage = notificationEntity.ExceptionMessage,
                WarningMessage = notificationEntity.WarningMessage,
            };

            return this.Ok(result);
        }
    }
}
