// <copyright file="GlobalSendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository for the entity that holds metadata for all sending operations in the table storage.
    /// </summary>
    public class GlobalSendingNotificationDataRepository : BaseRepository<GlobalSendingNotificationDataEntity>
    {
        private static readonly string GlobalSendingNotificationDataRowKey = "GlobalSendingNotificationData";

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalSendingNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public GlobalSendingNotificationDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                  configuration,
                  PartitionKeyNames.NotificationDataTable.TableName,
                  PartitionKeyNames.NotificationDataTable.GlobalSendingNotificationDataPartition,
                  isFromAzureFunction)
        {
        }

        /// <summary>
        /// Gets the entity that holds metadata for all sending operations.
        /// </summary>
        /// <returns>The Global Sending Notification Data Entity</returns>
        public async Task<GlobalSendingNotificationDataEntity> GetGlobalSendingNotificationDataEntity()
        {
            return await this.GetAsync(
                PartitionKeyNames.NotificationDataTable.GlobalSendingNotificationDataPartition,
                GlobalSendingNotificationDataRepository.GlobalSendingNotificationDataRowKey);
        }

        /// <summary>
        /// Insert or merges the entity that holds metadata for all sending operations. Partition Key and Row Key do not need to be
        /// set on the incoming entity.
        /// </summary>
        /// <param name="globalSendingNotificationDataEntity">Entity that holds metadata for all sending operations. Partition Key and
        /// Row Key do not need to be set.</param>
        /// <returns>The Task</returns>
        public async Task SetGlobalSendingNotificationDataEntity(GlobalSendingNotificationDataEntity globalSendingNotificationDataEntity)
        {
            globalSendingNotificationDataEntity.PartitionKey = PartitionKeyNames.NotificationDataTable.GlobalSendingNotificationDataPartition;
            globalSendingNotificationDataEntity.RowKey = GlobalSendingNotificationDataRepository.GlobalSendingNotificationDataRowKey;

            await this.InsertOrMergeAsync(globalSendingNotificationDataEntity);
        }
    }
}