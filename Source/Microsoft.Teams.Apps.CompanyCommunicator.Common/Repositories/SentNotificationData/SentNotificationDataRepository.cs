// <copyright file="SentNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Repository of the notification data in the table storage.
    /// </summary>
    public class SentNotificationDataRepository : BaseRepository<SentNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public SentNotificationDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                configuration,
                PartitionKeyNames.SentNotificationDataTable.TableName,
                PartitionKeyNames.SentNotificationDataTable.DefaultPartition,
                isFromAzureFunction)
        {
        }

        /// <summary>
        /// Initialize sent notification data for a recipient batch.
        /// Set status to be 0 (initial).
        /// </summary>
        /// <param name="notificationDataEntityId">Notification data entity id.</param>
        /// <param name="recipientDataBatch">A recipient data batch.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task InitializeSentNotificationDataForRecipientBatchAsync(
            string notificationDataEntityId,
            IEnumerable<UserDataEntity> recipientDataBatch)
        {
            var sentNotificationDataEntities = recipientDataBatch
                .Select(p =>
                    new SentNotificationDataEntity
                    {
                        PartitionKey = notificationDataEntityId,
                        RowKey = p.AadId,
                        AadId = p.AadId,
                        StatusCode = 0,
                        ConversationId = p.ConversationId,
                        TenantId = p.TenantId,
                        UserId = p.UserId,
                        ServiceUrl = p.ServiceUrl,
                    });

            await this.BatchInsertOrMergeAsync(sentNotificationDataEntities);
        }
    }
}