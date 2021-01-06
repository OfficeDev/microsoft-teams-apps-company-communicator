// <copyright file="UserDataTableNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData
{
    /// <summary>
    /// User data table names.
    /// </summary>
    public static class UserDataTableNames
    {
        /// <summary>
        /// Table name for the user data table.
        /// </summary>
        public static readonly string TableName = "UserData";

        /// <summary>
        /// Users data partition key name.
        /// </summary>
        public static readonly string UserDataPartition = "UserData";

        /// <summary>
        /// Users sync data partition.
        /// </summary>
        public static readonly string UsersSyncDataPartition = "UsersSyncData";

        /// <summary>
        /// All users delta link row key.
        /// </summary>
        public static readonly string AllUsersDeltaLinkRowKey = "AllUsersDeltaLink";

        /// <summary>
        /// Authors data partition key name.
        /// </summary>
        public static readonly string AuthorDataPartition = "AuthorData";
    }
}
