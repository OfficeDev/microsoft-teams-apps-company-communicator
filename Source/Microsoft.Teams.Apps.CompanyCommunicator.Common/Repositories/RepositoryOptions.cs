// <copyright file="RepositoryOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    /// <summary>
    /// Options used for creating repositories.
    /// </summary>
    public class RepositoryOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryOptions"/> class.
        /// </summary>
        public RepositoryOptions()
        {
            // Default this option to false because ensuring the table exists is technically
            // more robust.
            this.IsItExpectedThatTableAlreadyExists = false;
        }

        /// <summary>
        /// Gets or sets the storage account connection string.
        /// </summary>
        public string StorageAccountConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is expected that the table already exists.
        /// If the table already exists but the call is made to ensure it exists, then it results
        /// in an unnecessary failure request written in the logs. This flag is used to reduce
        /// the number of those unnecessary failure requests when it is expected that the table
        /// already exists.
        /// </summary>
        public bool IsItExpectedThatTableAlreadyExists { get; set; }
    }
}
