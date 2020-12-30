// <copyright file="GlobalSendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository for the entity that holds meta-data for all sending operations in the table storage.
    /// </summary>
    public class GlobalSendingNotificationDataRepository : BaseRepository<GlobalSendingNotificationDataEntity>, IGlobalSendingNotificationDataRepository
    {
        private static readonly string GlobalSendingNotificationDataRowKey = "GlobalSendingNotificationData";

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalSendingNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public GlobalSendingNotificationDataRepository(
            ILogger<GlobalSendingNotificationDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: NotificationDataTableNames.TableName,
                  defaultPartitionKey: NotificationDataTableNames.GlobalSendingNotificationDataPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
        }

        /// <inheritdoc/>
        public async Task<GlobalSendingNotificationDataEntity> GetGlobalSendingNotificationDataEntityAsync()
        {
            return await this.GetAsync(
                NotificationDataTableNames.GlobalSendingNotificationDataPartition,
                GlobalSendingNotificationDataRepository.GlobalSendingNotificationDataRowKey);
        }

        /// <inheritdoc/>
        public async Task SetGlobalSendingNotificationDataEntityAsync(GlobalSendingNotificationDataEntity globalSendingNotificationDataEntity)
        {
            globalSendingNotificationDataEntity.PartitionKey = NotificationDataTableNames.GlobalSendingNotificationDataPartition;
            globalSendingNotificationDataEntity.RowKey = GlobalSendingNotificationDataRepository.GlobalSendingNotificationDataRowKey;

            await this.InsertOrMergeAsync(globalSendingNotificationDataEntity);
        }
    }
}
