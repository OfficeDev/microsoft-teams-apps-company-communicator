﻿// <copyright file="NotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;

    /// <summary>
    /// Repository of the notification data in the table storage.
    /// </summary>
    public class NotificationDataRepository : BaseRepository<NotificationDataEntity>, INotificationDataRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public NotificationDataRepository(
            ILogger<NotificationDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions,
            TableRowKeyGenerator tableRowKeyGenerator)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: NotificationDataTableNames.TableName,
                  defaultPartitionKey: NotificationDataTableNames.DraftNotificationsPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
            this.TableRowKeyGenerator = tableRowKeyGenerator;
        }

        /// <inheritdoc/>
        public TableRowKeyGenerator TableRowKeyGenerator { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<NotificationDataEntity>> GetAllDraftNotificationsAsync()
        {
            string strFilter = TableQuery.GenerateFilterConditionForBool("IsScheduled", QueryComparisons.Equal, false);
            var result = await this.GetWithFilterAsync(strFilter, NotificationDataTableNames.DraftNotificationsPartition);

            return result;
        }

        /// <summary>
        /// Gets  scheduled notifications filtered by specific ChannelId.
        /// </summary>
        /// <param name="channelId">Channel Id to filter.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IEnumerable<NotificationDataEntity>> GetChannelScheduledNotificationsAsync(string channelId)
        {
            string strFilter1 = TableQuery.GenerateFilterConditionForBool("IsScheduled", QueryComparisons.Equal, true);
            string strFilter2 = TableQuery.GenerateFilterCondition("ChannelId", QueryComparisons.Equal, channelId);
            string strFilter = TableQuery.CombineFilters(strFilter1, TableOperators.And, strFilter2);

            var result = await this.GetWithFilterAsync(strFilter, NotificationDataTableNames.DraftNotificationsPartition);

            return result;
        }

        /// <summary>
        /// Gets all draft notifications filtered by specific ChannelId.
        /// </summary>
        /// <param name="channelId">Channel Id to filter.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IEnumerable<NotificationDataEntity>> GetChannelDraftNotificationsAsync(string channelId)
        {
            string strFilter1 = TableQuery.GenerateFilterConditionForBool("IsScheduled", QueryComparisons.Equal, false);
            string strFilter2 = TableQuery.GenerateFilterCondition("ChannelId", QueryComparisons.Equal, channelId);
            string strFilter = TableQuery.CombineFilters(strFilter1, TableOperators.And, strFilter2);

            var result = await this.GetWithFilterAsync(strFilter, NotificationDataTableNames.DraftNotificationsPartition);

            return result;
        }

        /// <summary>
        /// Get all scheduled notification entities from the table storage. Scheduled notifications are draft notifications with IsScheduled equal true.
        /// </summary>
        /// <returns>All scheduled notification entities.</returns>
        public async Task<IEnumerable<NotificationDataEntity>> GetAllScheduledNotificationsAsync()
        {
            string strFilter = TableQuery.GenerateFilterConditionForBool("IsScheduled", QueryComparisons.Equal, true);
            var result = await this.GetWithFilterAsync(strFilter, NotificationDataTableNames.DraftNotificationsPartition);

            return result;
        }

        /// <summary>
        /// Get all pending scheduled notification entities from the table storage. Pending Scheduled notifications are draft notifications with IsScheduled equal true and scheduled date previous than now.
        /// </summary>
        /// <returns>All pending scheduled notification entities.</returns>
        public async Task<IEnumerable<NotificationDataEntity>> GetAllPendingScheduledNotificationsAsync()
        {
            DateTime now = DateTime.UtcNow;
            string filter1 = TableQuery.GenerateFilterConditionForBool("IsScheduled", QueryComparisons.Equal, true);
            string filter2 = TableQuery.GenerateFilterConditionForDate("ScheduledDate", QueryComparisons.LessThanOrEqual, now);
            string filter = TableQuery.CombineFilters(filter1, TableOperators.And, filter2);

            var result = await this.GetWithFilterAsync(filter, NotificationDataTableNames.DraftNotificationsPartition);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<NotificationDataEntity>> GetMostRecentSentNotificationsAsync()
        {
            var result = await this.GetAllAsync(NotificationDataTableNames.SentNotificationsPartition, 20);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<NotificationDataEntity>> GetMostRecentChannelSentNotificationsAsync(string channelId)
        {
            string strFilter = TableQuery.GenerateFilterCondition("ChannelId", QueryComparisons.Equal, channelId);

            var result = await this.GetWithFilterAsync(strFilter, NotificationDataTableNames.SentNotificationsPartition, 20);

            return result;
        }

        /// <inheritdoc/>
        public async Task<string> MoveDraftToSentPartitionAsync(NotificationDataEntity draftNotificationEntity)
        {
            try
            {
                if (draftNotificationEntity == null)
                {
                    throw new ArgumentNullException(nameof(draftNotificationEntity));
                }

                var newSentNotificationId = this.TableRowKeyGenerator.CreateNewKeyOrderingMostRecentToOldest();

                // Create a sent notification based on the draft notification.
                var sentNotificationEntity = new NotificationDataEntity
                {
                    PartitionKey = NotificationDataTableNames.SentNotificationsPartition,
                    RowKey = newSentNotificationId,
                    Id = newSentNotificationId,
                    Title = draftNotificationEntity.Title,
                    ImageLink = draftNotificationEntity.ImageLink,
                    Summary = draftNotificationEntity.Summary,
                    Author = draftNotificationEntity.Author,
                    ButtonTitle = draftNotificationEntity.ButtonTitle,
                    ButtonLink = draftNotificationEntity.ButtonLink,
                    Buttons = draftNotificationEntity.Buttons,
                    CreatedBy = draftNotificationEntity.CreatedBy,
                    CreatedDate = draftNotificationEntity.CreatedDate,
                    ChannelId = draftNotificationEntity.ChannelId,
                    ChannelImage = draftNotificationEntity.ChannelImage,
                    ChannelTitle = draftNotificationEntity.ChannelTitle,
                    SentDate = null,
                    IsDraft = false,
                    IsImportant = draftNotificationEntity.IsImportant,
                    IsScheduled = draftNotificationEntity.IsScheduled,
                    ScheduledDate = draftNotificationEntity.ScheduledDate,
                    Teams = draftNotificationEntity.Teams,
                    Rosters = draftNotificationEntity.Rosters,
                    Groups = draftNotificationEntity.Groups,
                    CsvUsers = draftNotificationEntity.CsvUsers,
                    AllUsers = draftNotificationEntity.AllUsers,
                    MessageVersion = draftNotificationEntity.MessageVersion,
                    Succeeded = 0,
                    Failed = 0,
                    Throttled = 0,
                    TotalMessageCount = draftNotificationEntity.TotalMessageCount,
                    SendingStartedDate = DateTime.UtcNow,
                    Status = NotificationStatus.Queued.ToString(),
                    TrackingUrl = draftNotificationEntity.TrackingUrl,
                };
                await this.CreateOrUpdateAsync(sentNotificationEntity);

                // Delete the draft notification.
                await this.DeleteAsync(draftNotificationEntity);

                return newSentNotificationId;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DuplicateDraftNotificationAsync(
            NotificationDataEntity notificationEntity,
            string createdBy)
        {
            try
            {
                var newId = this.TableRowKeyGenerator.CreateNewKeyOrderingOldestToMostRecent();

                // TODO: Set the string "(copy)" in a resource file for multi-language support.
                var newNotificationEntity = new NotificationDataEntity
                {
                    PartitionKey = NotificationDataTableNames.DraftNotificationsPartition,
                    RowKey = newId,
                    Id = newId,
                    Title = notificationEntity.Title,
                    ImageLink = notificationEntity.ImageLink,
                    Summary = notificationEntity.Summary,
                    Author = notificationEntity.Author,
                    ButtonTitle = notificationEntity.ButtonTitle,
                    ButtonLink = notificationEntity.ButtonLink,
                    Buttons = notificationEntity.Buttons,
                    IsImportant = notificationEntity.IsImportant,
                    CreatedBy = createdBy,
                    CreatedDate = DateTime.UtcNow,
                    IsDraft = true,
                    Teams = notificationEntity.Teams,
                    Groups = notificationEntity.Groups,
                    Rosters = notificationEntity.Rosters,
                    AllUsers = notificationEntity.AllUsers,
                    CsvUsers = notificationEntity.CsvUsers,
                    TrackingUrl = notificationEntity.TrackingUrl,
                    ChannelId = notificationEntity.ChannelId,
                    ChannelTitle = notificationEntity.ChannelTitle,
                    ChannelImage = notificationEntity.ChannelImage,
                };

                await this.CreateOrUpdateAsync(newNotificationEntity);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateNotificationStatusAsync(string notificationId, NotificationStatus status)
        {
            var notificationDataEntity = await this.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                notificationId);

            if (notificationDataEntity != null)
            {
                notificationDataEntity.Status = status.ToString();
                await this.CreateOrUpdateAsync(notificationDataEntity);
            }
        }

        /// <inheritdoc/>
        public async Task SaveExceptionInNotificationDataEntityAsync(
            string notificationDataEntityId,
            string errorMessage)
        {
            var notificationDataEntity = await this.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                var newMessage = notificationDataEntity.ErrorMessage.AppendNewLine(errorMessage);

                // Restrict the total length of stored message to avoid hitting table storage limits
                if (newMessage.Length <= MaxMessageLengthToSave)
                {
                    notificationDataEntity.ErrorMessage = newMessage;
                }

                notificationDataEntity.Status = NotificationStatus.Failed.ToString();

                // Set the end date as current date.
                notificationDataEntity.SentDate = DateTime.UtcNow;

                await this.CreateOrUpdateAsync(notificationDataEntity);
            }
        }

        /// <inheritdoc/>
        public async Task SaveWarningInNotificationDataEntityAsync(
            string notificationDataEntityId,
            string warningMessage)
        {
            try
            {
                var notificationDataEntity = await this.GetAsync(
                    NotificationDataTableNames.SentNotificationsPartition,
                    notificationDataEntityId);
                if (notificationDataEntity != null)
                {
                    var newMessage = notificationDataEntity.WarningMessage.AppendNewLine(warningMessage);

                    // Restrict the total length of stored message to avoid hitting table storage limits
                    if (newMessage.Length <= MaxMessageLengthToSave)
                    {
                        notificationDataEntity.WarningMessage = newMessage;
                    }

                    await this.CreateOrUpdateAsync(notificationDataEntity);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
