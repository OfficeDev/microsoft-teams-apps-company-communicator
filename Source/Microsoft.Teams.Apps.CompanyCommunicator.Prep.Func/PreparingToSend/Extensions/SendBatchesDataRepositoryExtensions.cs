// <copyright file="SendBatchesDataRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData;

    /// <summary>
    /// Extension methods for the SendBatchesDataRepository class.
    /// </summary>
    public static class SendBatchesDataRepositoryExtensions
    {
        /// <summary>
        /// Gets the partition key for a batch in the SendBatchesData table by appending
        /// a recipient data batch suffix to the notification Id.
        /// </summary>
        /// <param name="sendBatchesDataRepository">The send batches data repository.</param>
        /// <param name="notificationId">The notification Id.</param>
        /// <param name="batchIndex">The batch index.</param>
        /// <returns>The partition key of the corresponding batch.</returns>
        public static string GetBatchPartitionKey(
            this SendBatchesDataRepository sendBatchesDataRepository,
            string notificationId,
            int batchIndex)
        {
            return $"{notificationId}-batch{batchIndex}";
        }
    }
}
