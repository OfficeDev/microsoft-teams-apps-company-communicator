// <copyright file="BaseRepository.cs" company="Microsoft">
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
    /// <typeparam name="T">Entity class type.</typeparam>
    public class BaseRepository<T>
        where T : TableEntity, new()
    {
        private readonly CloudTable notificationTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="tableName">Table name.</param>
        public BaseRepository(IConfiguration configuration, string tableName)
        {
            var storageAccountSASConnectionString = configuration.GetValue<string>("StorageAccountSASConnectionString");
            var storageAccount = CloudStorageAccount.Parse(storageAccountSASConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            this.notificationTable = tableClient.GetTableReference(tableName);
        }

        /// <summary>
        /// Get all data entities from the table storage.
        /// </summary>
        /// <returns>All data entities.</returns>
        public IEnumerable<T> All()
        {
            var query = new TableQuery<T>();

            var entities = this.notificationTable.ExecuteQuery<T>(query);

            return entities.ToList();
        }

        /// <summary>
        /// Create or update an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be created or updated.</param>
        public void CreateOrUpdate(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);

            this.notificationTable.Execute(operation);
        }

        /// <summary>
        /// Delete an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be deleted.</param>
        public void Delete(T entity)
        {
            var operation = TableOperation.Delete(entity);

            this.notificationTable.Execute(operation);
        }

        /// <summary>
        /// Get an entity by the keys in the table storage.
        /// </summary>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key fo the entity.</param>
        /// <returns>The entity matching the keys.</returns>
        public T Get(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var result = this.notificationTable.Execute(operation);

            return result.Result as T;
        }

        /// <summary>
        /// Get all data entities from the table storage.
        /// </summary>
        /// <param name="filter">Filter to the result.</param>
        /// <returns>All data entities.</returns>
        protected IEnumerable<T> All(string filter)
        {
            var query = new TableQuery<T>().Where(filter);

            var entities = this.notificationTable.ExecuteQuery<T>(query);

            return entities.ToList();
        }
    }
}
