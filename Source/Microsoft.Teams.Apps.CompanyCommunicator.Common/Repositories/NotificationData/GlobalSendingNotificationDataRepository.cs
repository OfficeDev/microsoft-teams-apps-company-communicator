// <copyright file="GlobalSendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository for the entity that holds metadata for all sending operations in the table storage.
    /// </summary>
    public class GlobalSendingNotificationDataRepository : BaseRepository<GlobalSendingNotificationDataEntity>
    {
        private static readonly string GlobalSendingNotificationDataRowKey = "GlobalSendingNotificationData";

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalSendingNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public GlobalSendingNotificationDataRepository(IOptions<RepositoryOptions> repositoryOptions)
            : base(
                storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                tableName: NotificationDataTableNames.TableName,
                defaultPartitionKey: NotificationDataTableNames.GlobalSendingNotificationDataPartition,
                isItExpectedThatTableAlreadyExists: repositoryOptions.Value.IsItExpectedThatTableAlreadyExists)
        {
        }

        /// <summary>
        /// Gets the entity that holds metadata for all sending operations.
        /// </summary>
        /// <returns>The Global Sending Notification Data Entity.</returns>
        public async Task<GlobalSendingNotificationDataEntity> GetGlobalSendingNotificationDataEntityAsync()
        {
            return await this.GetAsync(
                NotificationDataTableNames.GlobalSendingNotificationDataPartition,
                GlobalSendingNotificationDataRepository.GlobalSendingNotificationDataRowKey);
        }

        /// <summary>
        /// Insert or merges the entity that holds metadata for all sending operations. Partition Key and Row Key do not need to be
        /// set on the incoming entity.
        /// </summary>
        /// <param name="globalSendingNotificationDataEntity">Entity that holds metadata for all sending operations. Partition Key and
        /// Row Key do not need to be set.</param>
        /// <returns>The Task.</returns>
        public async Task SetGlobalSendingNotificationDataEntityAsync(GlobalSendingNotificationDataEntity globalSendingNotificationDataEntity)
        {
            globalSendingNotificationDataEntity.PartitionKey = NotificationDataTableNames.GlobalSendingNotificationDataPartition;
            globalSendingNotificationDataEntity.RowKey = GlobalSendingNotificationDataRepository.GlobalSendingNotificationDataRowKey;

            await this.InsertOrMergeAsync(globalSendingNotificationDataEntity);
        }
    }
}
