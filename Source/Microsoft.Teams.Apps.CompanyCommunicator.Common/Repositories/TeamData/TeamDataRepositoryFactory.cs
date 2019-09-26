// <copyright file="TeamDataRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository factory service.
    /// </summary>
    public class TeamDataRepositoryFactory
    {
        private readonly IConfiguration configuration;
        private TeamDataRepository teamDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataRepositoryFactory"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public TeamDataRepositoryFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Create repository service instance.
        /// </summary>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        /// <returns>It returns a repository service instance.</returns>
        public TeamDataRepository CreateRepository(bool isFromAzureFunction = false)
        {
            if (this.teamDataRepository == null)
            {
                this.teamDataRepository = new TeamDataRepository(
                    this.configuration,
                    isFromAzureFunction);
            }

            return this.teamDataRepository;
        }
    }
}
