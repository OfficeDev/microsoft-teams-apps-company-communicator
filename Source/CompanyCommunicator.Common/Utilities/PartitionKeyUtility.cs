// <copyright file="PartitionKeyUtility.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Utilities
{
    using System;

    /// <summary>
    /// Partition Key utility.
    /// </summary>
    public static class PartitionKeyUtility
    {
        /// <summary>
        /// Create the partition key from notification id.
        /// </summary>
        /// <param name="notificationId">notification id.</param>
        /// <param name="batchIndex">batch index.</param>
        /// <returns>partition key.</returns>
        public static string CreateBatchPartitionKey(string notificationId, int batchIndex)
        {
            return $"{notificationId}:{batchIndex}";
        }

        /// <summary>
        /// Get the notification id from partition key.
        /// </summary>
        /// <param name="partitionKey">partition key.</param>
        /// <returns>notification id.</returns>
        public static string GetNotificationIdFromBatchPartitionKey(string partitionKey)
        {
            var result = partitionKey.Split(":");
            if (result.Length != 2)
            {
                throw new FormatException("Invalid format of batch partition key");
            }

            return result[0];
        }

        /// <summary>
        /// Get the notification id from partition key.
        /// </summary>
        /// <param name="partitionKey">partition key.</param>
        /// <returns>notification id.</returns>
        public static string GetBatchIdFromBatchPartitionKey(string partitionKey)
        {
            var result = partitionKey.Split(":");
            if (result.Length != 2)
            {
                throw new FormatException("Invalid format of batch partition key");
            }

            return result[1];
        }
    }
}
