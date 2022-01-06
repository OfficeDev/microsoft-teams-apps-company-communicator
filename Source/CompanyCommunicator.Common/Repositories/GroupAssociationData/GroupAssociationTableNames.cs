// <copyright file="GroupAssociationTableNames.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.GroupAssociationData
{
    /// <summary>
    /// Group Association data table names.
    /// </summary>
    public static class GroupAssociationTableNames
    {
        /// <summary>
        /// Table name for the group association data table.
        /// </summary>
        public static readonly string TableName = "GroupAssociationData";

        /// <summary>
        /// Group association data partition key name.
        /// </summary>
        public static readonly string GroupDataPartition = "GroupData";
    }
}