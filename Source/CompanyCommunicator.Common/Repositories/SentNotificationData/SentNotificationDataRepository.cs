// <copyright file="SentNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;

    /// <summary>
    /// Repository of the notification data in the table storage.
    /// </summary>
    public class SentNotificationDataRepository : BaseRepository<SentNotificationDataEntity>, ISentNotificationDataRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SentNotificationDataRepository(
            ILogger<SentNotificationDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: SentNotificationDataTableNames.TableName,
                  defaultPartitionKey: SentNotificationDataTableNames.DefaultPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
        }

        /// <inheritdoc/>
        public async Task EnsureSentNotificationDataTableExistsAsync()
        {
            var exists = await this.Table.ExistsAsync();
            if (!exists)
            {
                await this.Table.CreateAsync();
            }
        }

        /// <inheritdoc/>
        public async Task SaveExceptionInSentNotificationDataEntityAsync(
           string notificationId,
           string recipientId,
           string errorMessage)
        {
            var sentNotificationDataEntity = await this.GetAsync(notificationId, recipientId);

            if (sentNotificationDataEntity == null)
            {
                return;
            }

            var newMessage = sentNotificationDataEntity.ErrorMessage.AppendNewLine(errorMessage);

            // Restrict the total length of stored message to avoid hitting table storage limits
            if (newMessage.Length <= MaxMessageLengthToSave)
            {
                sentNotificationDataEntity.ErrorMessage = newMessage;
                await this.InsertOrMergeAsync(sentNotificationDataEntity);
            }
        }
    }
}
