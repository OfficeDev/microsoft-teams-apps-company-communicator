// <copyright file="IRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Base repository Interface.
    /// </summary>
    /// <typeparam name="T">Entity class type.</typeparam>
    public interface IRepository<T>
        where T : TableEntity, new()
    {
        /// <summary>
        /// Gets cloud table instance.
        /// </summary>
        public CloudTable Table { get; }

        /// <summary>
        /// Create or update an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be created or updated.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public Task CreateOrUpdateAsync(T entity);

        /// <summary>
        /// Insert or merge an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be inserted or merged.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public Task InsertOrMergeAsync(T entity);

        /// <summary>
        /// Delete an entity in the table storage.
        /// </summary>
        /// <param name="entity">Entity to be deleted.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public Task DeleteAsync(T entity);

        /// <summary>
        /// Get an entity by the keys in the table storage.
        /// </summary>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key for the entity.</param>
        /// <returns>The entity matching the keys.</returns>
        public Task<T> GetAsync(string partitionKey, string rowKey);

        /// <summary>
        /// Get entities from the table storage in a partition with a filter.
        /// </summary>
        /// <param name="filter">Filter to the result.</param>
        /// <param name="partition">Partition key value.</param>
        /// <returns>All data entities.</returns>
        public Task<IEnumerable<T>> GetWithFilterAsync(string filter, string partition = null);

        /// <summary>
        /// Get all data entities from the table storage in a partition.
        /// </summary>
        /// <param name="partition">Partition key value.</param>
        /// <param name="count">The max number of desired entities.</param>
        /// <returns>All data entities.</returns>
        public Task<IEnumerable<T>> GetAllAsync(string partition = null, int? count = null);

        /// <summary>
        /// Get all data entities from the table storage.
        /// </summary>
        /// <param name="partition">Partition key value.</param>
        /// <param name="count">The max number of desired entities.</param>
        /// <returns>All data entities.</returns>
        public Task<IEnumerable<T>> GetAllDeleteAsync(string partition = null, int? count = null);

        /// <summary>
        /// Get paged data entities from the table storage in a partition.
        /// </summary>
        /// <param name="partition">Partition key value.</param>
        /// <param name="count">The max number of desired entities.</param>
        /// <param name="token">The continuation token.</param>
        /// <returns>All data entities and continuation token.</returns>
        Task<(IEnumerable<T>, TableContinuationToken)> GetPagedAsync(string partition = null, int? count = null, TableContinuationToken token = null);

        /// <summary>
        /// Get filtered data entities by date time from the table storage.
        /// </summary>
        /// <param name="startDateTime">Start date time.</param>
        /// <param name="endDateTime">End date time.</param>
        /// <returns>Filtered data entities.</returns>
        public Task<IEnumerable<T>> GetAllBetweenDateTimesAsync(DateTime startDateTime, DateTime endDateTime);

        /// <summary>
        /// Get all data stream from the table storage in a partition.
        /// </summary>
        /// <param name="partition">Partition key value.</param>
        /// <param name="count">The max number of desired entities.</param>
        /// <returns>All data stream..</returns>
        public IAsyncEnumerable<IEnumerable<T>> GetStreamsAsync(string partition = null, int? count = null);

        /// <summary>
        /// Insert or merge a batch of entities in Azure table storage.
        /// A batch can contain up to 100 entities.
        /// </summary>
        /// <param name="entities">Entities to be inserted or merged in Azure table storage.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public Task BatchInsertOrMergeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Insert or merge a batch of entities in Azure table storage.
        /// A batch can contain up to 100 entities.
        /// </summary>
        /// <param name="entities">Entities to be inserted or merged in Azure table storage.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public Task BatchDeleteAsync(IEnumerable<T> entities);
    }
}