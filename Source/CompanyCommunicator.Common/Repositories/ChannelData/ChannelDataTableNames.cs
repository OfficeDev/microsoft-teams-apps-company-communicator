// <copyright file="ChannelDataTableNames.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ChannelData
{
    /// <summary>
    /// Channel data table names.
    /// </summary>
    public static class ChannelDataTableNames
    {
        /// <summary>
        /// Table name for the group association data table.
        /// </summary>
        public static readonly string TableName = "ChannelData";

        /// <summary>
        /// Group association data partition key name.
        /// </summary>
        public static readonly string ChannelDataPartition = "ChannelData";
    }
}