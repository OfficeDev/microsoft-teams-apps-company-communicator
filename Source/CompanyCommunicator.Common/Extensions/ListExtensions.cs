// <copyright file="ListExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// List Extension.
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
        /// <param name="batchSize">The batch size.</param>
        /// <returns>The batches (a list of lists).</returns>
        public static List<List<T>> SeparateIntoBatches<T>(this List<T> sourceList, int batchSize)
        {
            var batches = new List<List<T>>();

            var totalNumberOfEntities = sourceList.Count;
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
