// <copyright file="SendBatchesDataTableNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData
{
    /// <summary>
    /// Send batches data table names.
    /// </summary>
    public static class SendBatchesDataTableNames
    {
        /// <summary>
        /// Table name for the send batches data table.
        /// </summary>
        public static readonly string TableName = "SendBatchesData";

        /// <summary>
        /// Default partition - should not be used.
        /// </summary>
        public static readonly string DefaultPartition = "Default";
    }
}
