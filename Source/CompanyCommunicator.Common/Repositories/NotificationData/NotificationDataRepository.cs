// <copyright file="NotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Blob;

    /// <summary>
    /// Repository of the notification data in the table storage.
    /// </summary>
    public class NotificationDataRepository : BaseRepository<NotificationDataEntity>, INotificationDataRepository
    {
        private readonly IBlobStorageProvider storageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDataRepository"/> class.
        /// </summary>
        /// <param name="storageProvider">The blob storage provider.</param>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public NotificationDataRepository(
            IBlobStorageProvider storageProvider,
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
            this.storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            this.TableRowKeyGenerator = tableRowKeyGenerator;
        }

        /// <inheritdoc/>
        public TableRowKeyGenerator TableRowKeyGenerator { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<NotificationDataEntity>> GetAllDraftNotificationsAsync()
        {
            var result = await this.GetAllAsync(NotificationDataTableNames.DraftNotificationsPartition);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<NotificationDataEntity>> GetMostRecentSentNotificationsAsync()
        {
            var result = await this.GetAllAsync(NotificationDataTableNames.SentNotificationsPartition, 25);

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
                    ImageBase64BlobName = draftNotificationEntity.ImageBase64BlobName,
                    Summary = draftNotificationEntity.Summary,
                    Author = draftNotificationEntity.Author,
                    ButtonTitle = draftNotificationEntity.ButtonTitle,
                    ButtonLink = draftNotificationEntity.ButtonLink,
                    CreatedBy = draftNotificationEntity.CreatedBy,
                    CreatedDate = draftNotificationEntity.CreatedDate,
                    SentDate = null,
                    IsDraft = false,
                    Teams = draftNotificationEntity.Teams,
                    Rosters = draftNotificationEntity.Rosters,
                    Groups = draftNotificationEntity.Groups,
                    AllUsers = draftNotificationEntity.AllUsers,
                    MessageVersion = draftNotificationEntity.MessageVersion,
                    Succeeded = 0,
                    Failed = 0,
                    Throttled = 0,
                    TotalMessageCount = draftNotificationEntity.TotalMessageCount,
                    SendingStartedDate = DateTime.UtcNow,
                    Status = NotificationStatus.Queued.ToString(),
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
                    CreatedBy = createdBy,
                    CreatedDate = DateTime.UtcNow,
                    IsDraft = true,
                    Teams = notificationEntity.Teams,
                    Groups = notificationEntity.Groups,
                    Rosters = notificationEntity.Rosters,
                    AllUsers = notificationEntity.AllUsers,
                };

                if (!string.IsNullOrEmpty(notificationEntity.ImageBase64BlobName))
                {
                    await this.storageProvider.CopyImageBlobAsync(notificationEntity.ImageBase64BlobName, newId);
                    newNotificationEntity.ImageBase64BlobName = newId;
                }

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
                var newErrorMessage = this.AppendNewLine(notificationDataEntity.ErrorMessage, errorMessage);

                // Restrict the total length of stored message to avoid hitting table storage limits
                if (newErrorMessage.Length <= MaxMessageLengthToSave)
                {
                    notificationDataEntity.ErrorMessage = newErrorMessage;
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
                    var newWarningMessage = this.AppendNewLine(notificationDataEntity.WarningMessage, warningMessage);

                    // Restrict the total length of stored message to avoid hitting table storage limits
                    if (newWarningMessage.Length <= MaxMessageLengthToSave)
                    {
                        notificationDataEntity.WarningMessage = newWarningMessage;
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

        /// <inheritdoc/>
        public async Task<string> SaveImageAsync(string blobName, string base64Image)
        {
            return await this.storageProvider.UploadBase64ImageAsync(blobName, base64Image);
        }

        /// <inheritdoc/>
        public async Task<string> GetImageAsync(string prefix, string blobName)
        {
            return prefix + await this.storageProvider.DownloadBase64ImageAsync(blobName);
        }

        private string AppendNewLine(string originalString, string newString)
        {
            return string.IsNullOrWhiteSpace(originalString)
                ? newString
                : $"{originalString}{Environment.NewLine}{newString}";
        }
    }
}