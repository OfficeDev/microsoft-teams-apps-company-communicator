// <copyright file="NotificationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository for the notification data in the table storage.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly CloudTable notificationTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public NotificationRepository(IConfiguration configuration)
        {
            var storageAccountSASConnectionString = configuration.GetValue<string>("StorageAccountSASConnectionString");
            var storageAccount = CloudStorageAccount.Parse(storageAccountSASConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            this.notificationTable = tableClient.GetTableReference("Notification");
        }

        /// <inheritdoc/>
        public IEnumerable<NotificationEntity> All(bool isDraft)
        {
            var query = new TableQuery<NotificationEntity>()
                .Where(TableQuery.GenerateFilterConditionForBool(
                    nameof(NotificationEntity.IsDraft),
                    QueryComparisons.Equal,
                    isDraft));

            var entities = this.notificationTable.ExecuteQuery(query).ToList();

            return entities;
        }

        /// <inheritdoc/>
        public void CreateOrUpdate(NotificationEntity entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);

            this.notificationTable.Execute(operation);
        }

        /// <inheritdoc/>
        public void Delete(NotificationEntity entity)
        {
            var operation = TableOperation.Delete(entity);

            this.notificationTable.Execute(operation);
        }

        /// <inheritdoc/>
        public NotificationEntity Get(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<NotificationEntity>(partitionKey, rowKey);

            var result = this.notificationTable.Execute(operation);

            return result.Result as NotificationEntity;
        }
    }
}
