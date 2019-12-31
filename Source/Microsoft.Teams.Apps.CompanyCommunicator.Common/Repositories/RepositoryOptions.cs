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
            this.IsAzureFunction = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the repository is for an Azure Function.
        /// </summary>
        public bool IsAzureFunction { get; set; }
    }
}
