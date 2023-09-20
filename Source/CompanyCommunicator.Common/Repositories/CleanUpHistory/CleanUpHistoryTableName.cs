// <copyright file="CleanUpHistoryTableName.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.CleanUpHistory
{
    /// <summary>
    /// CleanUpHistory table names.
    /// </summary>
    public class CleanUpHistoryTableName
    {
        /// <summary>
        /// Table name for the delete message history table.
        /// </summary>
        public static readonly string TableName = "CleanUpHistory";

        /// <summary>
        /// Default partition - should not be used.
        /// </summary>
        public static readonly string DefaultPartition = "Default";
    }
}
