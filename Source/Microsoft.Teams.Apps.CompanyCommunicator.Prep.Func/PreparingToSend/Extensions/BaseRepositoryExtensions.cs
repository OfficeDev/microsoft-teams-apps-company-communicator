// <copyright file="BaseRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// Extension methods for the BaseRepository class.
    /// </summary>
    public static class BaseRepositoryExtensions
    {
        /// <summary>
        /// Inserts or merges one batch of entities into the data table in one batch request.
        /// Note: The maximum size of one batch request to the data table is the same as the maximum size of
        /// a batch request to a Service Bus queue. Because this batch will be used to create a batch
        /// request to the Send queue, the Send queue's maximum size is used to verify the size of the batch.
        /// </summary>
        /// <typeparam name="T">A table entity.</typeparam>
        /// <param name="repository">The data repository.</param>
        /// <param name="entities">The batch of entities.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task InsertOrMergeOneBatchAsync<T>(
            this BaseRepository<T> repository,
            IList<T> entities)
            where T : TableEntity, new()
        {
            var maxBatchSize = SendQueue.MaxNumberOfMessagesInBatchRequest;

            if (entities.Count > maxBatchSize)
            {
                throw new ArgumentException($"A batch may not have more than {maxBatchSize} entities - given count: {entities.Count}");
            }

            var batchOperation = new TableBatchOperation();
            foreach (var entity in entities)
            {
                batchOperation.InsertOrMerge(entity);
            }

            await repository.Table.ExecuteBatchAsync(batchOperation);
        }
    }
}
