// <copyright file="SendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Blob;

    /// <summary>
    /// Repository for the sending notification data in the table storage.
    /// </summary>
    public class SendingNotificationDataRepository : BaseRepository<SendingNotificationDataEntity>, ISendingNotificationDataRepository
    {
        private readonly IBlobStorageProvider storageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendingNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="storageProvider">The storage provider.</param>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SendingNotificationDataRepository(
            IBlobStorageProvider storageProvider,
            ILogger<SendingNotificationDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: NotificationDataTableNames.TableName,
                  defaultPartitionKey: NotificationDataTableNames.SendingNotificationsPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
            this.storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        }

        /// <inheritdoc/>
        public async Task<string> GetAdaptiveCardAsync(string blobName)
        {
            return await this.storageProvider.DownloadAdaptiveCardAsync(blobName);
        }

        /// <inheritdoc/>
        public async Task<string> GetImageAsync(string blobName)
        {
            return await this.storageProvider.DownloadBase64ImageAsync(blobName);
        }

        /// <inheritdoc/>
        public async Task SaveAdaptiveCardAsync(string blobName, string adaptiveCard)
        {
            await this.storageProvider.UploadAdaptiveCardAsync(blobName, adaptiveCard);
        }
    }
}