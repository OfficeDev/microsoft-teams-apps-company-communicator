// <copyright file="BaseRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Base respository for the data stored in the Azure Table Storage.
    /// </summary>
    /// <typeparam name="T">Entity class type.</typeparam>
    public class BaseRepository<T>
        where T : TableEntity, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="tableName">The name of the table in Azure Table Storage.</param>
        public BaseRepository(IConfiguration configuration, string tableName)
        {
            var storageAccountConnectionString = configuration["StorageAccountConnectionString"];
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            this.Table = tableClient.GetTableReference(tableName);
            this.Table.CreateIfNotExists();
        }

        /// <summary>
        /// Gets cloud table instance.
        /// </summary>
        protected CloudTable Table { get; }

        /// <summary>
        /// Get all data entities from the table storage.
        /// </summary>
        /// <returns>All data entities.</returns>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await Task.Run(() =>
            {
                var query = new TableQuery<T>();

                var entities = this.Table.ExecuteQuery<T>(query);

                return entities.ToList();
            });
        }

        /// <summary>
        /// Create or update an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be created or updated.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task CreateOrUpdateAsync(T entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);

            await this.Table.ExecuteAsync(operation);
        }

        /// <summary>
        /// Delete an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be deleted.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task DeleteAsync(T entity)
        {
            var operation = TableOperation.Delete(entity);

            await this.Table.ExecuteAsync(operation);
        }

        /// <summary>
        /// Get an entity by the keys in the table storage.
        /// </summary>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key fo the entity.</param>
        /// <returns>The entity matching the keys.</returns>
        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var result = await this.Table.ExecuteAsync(operation);

            return result.Result as T;
        }

        /// <summary>
        /// Get all data entities from the table storage.
        /// </summary>
        /// <param name="filter">Filter to the result.</param>
        /// <returns>All data entities.</returns>
        protected async Task<IEnumerable<T>> GetAllAsync(string filter)
        {
            return await Task.Run(() =>
            {
                var query = new TableQuery<T>().Where(filter);

                var entities = this.Table.ExecuteQuery<T>(query);

                return entities.ToList();
            });
        }
    }
}
