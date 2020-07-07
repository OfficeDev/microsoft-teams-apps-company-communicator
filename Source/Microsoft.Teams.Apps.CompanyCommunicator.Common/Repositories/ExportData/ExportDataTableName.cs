// <copyright file="ExportDataTableName.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData
{
    /// <summary>
    /// Export data table names.
    /// </summary>
    public class ExportDataTableName
    {
        /// <summary>
        /// Table name for the send batches data table.
        /// </summary>
        public static readonly string TableName = "ExportData";

        /// <summary>
        /// Default partition - should not be used.
        /// </summary>
        public static readonly string DefaultPartition = "Default";
    }
}
