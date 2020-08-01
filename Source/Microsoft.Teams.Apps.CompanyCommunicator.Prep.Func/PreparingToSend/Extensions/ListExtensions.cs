// <copyright file="ListExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// Extension methods for the List class.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Extension method to separate a list of objects into batches (a list of lists).
        /// The size of the batch is determined by the maximum allowed size of a batch
        /// request to the Send queue service bus queue.
        /// </summary>
        /// <typeparam name="T">An object type.</typeparam>
        /// <param name="sourceList">The list to break into batches.</param>
        /// <returns>The batches (a list of lists).</returns>
        public static List<List<T>> SeparateIntoBatches<T>(this List<T> sourceList)
        {
            var batches = new List<List<T>>();

            var totalNumberOfEntities = sourceList.Count;

            // Use the SendQueue's maximum number of messages in a batch request number because
            // the list is being broken into batches in order to be added to that queue.
            var batchSize = SendQueue.MaxNumberOfMessagesInBatchRequest;
            var numberOfCompleteBatches = totalNumberOfEntities / batchSize;
            var numberOfEntitiesInIncompleteBatch = totalNumberOfEntities % batchSize;

            for (var i = 0; i < numberOfCompleteBatches; i++)
            {
                var startingIndex = i * batchSize;
                var batch = sourceList.GetRange(startingIndex, batchSize);
                batches.Add(batch);
            }

            if (numberOfEntitiesInIncompleteBatch != 0)
            {
                var incompleteBatchStartingIndex = numberOfCompleteBatches * batchSize;
                var incompleteBatch = sourceList.GetRange(
                    incompleteBatchStartingIndex,
                    numberOfEntitiesInIncompleteBatch);
                batches.Add(incompleteBatch);
            }

            return batches;
        }
    }
}
