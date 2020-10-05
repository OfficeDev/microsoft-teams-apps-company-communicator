// <copyright file="EnumerableExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// IEnumerable Extension.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Check if the list is null or empty.
        /// </summary>
        /// <typeparam name="T">entity class type.</typeparam>
        /// <param name="enumerable">the list of types.</param>
        /// <returns>Indicating if the list is empty or null.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable?.Any() ?? true;
        }

        /// <summary>
        /// ForEachAsync implementation to invoke body for each element.
        /// It partitions the input to N partitions, N being the maximum degree of parallelism.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="source">Source.</param>
        /// <param name="maxParallelism">max degree of parallelism.</param>
        /// <param name="body">Body.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int maxParallelism, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(maxParallelism)
                select Task.Run(async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await body(partition.Current);
                        }
                    }
                }));
        }
    }
}
