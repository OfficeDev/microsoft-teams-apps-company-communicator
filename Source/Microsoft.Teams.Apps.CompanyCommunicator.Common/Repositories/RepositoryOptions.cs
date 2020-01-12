// <copyright file="RepositoryOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    /// <summary>
    /// Options class used for creating repository objects.
    /// </summary>
    public class RepositoryOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryOptions"/> class.
        /// </summary>
        public RepositoryOptions()
        {
            // Default this option to false.
            this.IsExpectedTableAlreadyExist = false;
        }

        /// <summary>
        /// Gets or sets the storage account connection string.
        /// </summary>
        public string StorageAccountConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is expected that the table already exists.
        /// </summary>
        public bool IsExpectedTableAlreadyExist { get; set; }
    }
}
