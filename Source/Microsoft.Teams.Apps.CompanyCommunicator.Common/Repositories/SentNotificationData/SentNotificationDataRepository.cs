// <copyright file="SentNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Repository of the notification data in the table storage.
    /// </summary>
    public class SentNotificationDataRepository : BaseRepository<SentNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SentNotificationDataRepository(IOptions<RepositoryOptions> repositoryOptions)
            : base(
                storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                tableName: SentNotificationDataTableNames.TableName,
                defaultPartitionKey: SentNotificationDataTableNames.DefaultPartition,
                isItExpectedThatTableAlreadyExists: repositoryOptions.Value.IsItExpectedThatTableAlreadyExists)
        {
        }

        /// <summary>
        /// This method ensures the SentNotificationData table is create in the storage.
        /// This method should be called before kicking off an Azure function that uses the SentNotificationData table.
        /// Otherwise the app will crash.
        /// By design, Azure functions (in this app) does not create a table if it's absent.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task EnsureSentNotificationDataTableExistingAsync()
        {
            var exists = await this.Table.ExistsAsync();
            if (!exists)
            {
                await this.Table.CreateAsync();
            }
        }

        /// <summary>
        /// Initializes the sent notification data for a recipient batch of users.
        /// Sets the status code to be the initialized value.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch of users.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task InitializeSentNotificationDataForUserRecipientBatchAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataBatch)
        {
            // Create the SentNotificationDataEntity from the incoming UserDataEntity.
            var sentNotificationDataEntities = recipientDataBatch
                .Select(p =>
                    new SentNotificationDataEntity
                    {
                        PartitionKey = notificationDataEntityId,
                        RowKey = p.AadId,
                        RecipientType = SentNotificationDataEntity.UserRecipientType,
                        RecipientId = p.AadId,
                        StatusCode = SentNotificationDataEntity.InitializationStatusCode,
                        ConversationId = p.ConversationId,
                        TenantId = p.TenantId,
                        UserId = p.UserId,
                        ServiceUrl = p.ServiceUrl,
                    });

            await this.BatchInsertOrMergeAsync(sentNotificationDataEntities);
        }

        /// <summary>
        /// Initializes the sent notification data for a recipient batch of teams.
        /// Sets the status code to be the initialized value.
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch of teams.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task InitializeSentNotificationDataForTeamRecipientBatchAsync(
            string notificationDataEntityId,
            IEnumerable<TeamDataEntity> recipientDataBatch)
        {
            // Create the SentNotificationDataEntity from the incoming TeamDataEntity.
            var sentNotificationDataEntities = recipientDataBatch
                .Select(p =>
                    new SentNotificationDataEntity
                    {
                        PartitionKey = notificationDataEntityId,
                        RowKey = p.TeamId,
                        RecipientType = SentNotificationDataEntity.TeamRecipientType,
                        RecipientId = p.TeamId,
                        StatusCode = SentNotificationDataEntity.InitializationStatusCode,
                        ConversationId = p.TeamId,
                        TenantId = p.TenantId,
                        ServiceUrl = p.ServiceUrl,
                    });

            await this.BatchInsertOrMergeAsync(sentNotificationDataEntities);
        }
    }
}
