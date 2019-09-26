// <copyright file="UserDataRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository factory service.
    /// </summary>
    public class UserDataRepositoryFactory
    {
        private readonly IConfiguration configuration;
        private UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataRepositoryFactory"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public UserDataRepositoryFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Create repository service instance.
        /// </summary>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        /// <returns>It returns a repository service instance.</returns>
        public UserDataRepository CreateRepository(bool isFromAzureFunction = false)
        {
            if (this.userDataRepository == null)
            {
                this.userDataRepository = new UserDataRepository(
                    this.configuration,
                    isFromAzureFunction);
            }

            return this.userDataRepository;
        }
    }
}
