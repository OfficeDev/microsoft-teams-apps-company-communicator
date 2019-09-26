// <copyright file="SentNotificationDataRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository factory service.
    /// </summary>
    public class SentNotificationDataRepositoryFactory
    {
        private readonly IConfiguration configuration;
        private SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationDataRepositoryFactory"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public SentNotificationDataRepositoryFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Create repository service instance.
        /// </summary>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        /// <returns>It returns a repository service instance.</returns>
        public SentNotificationDataRepository CreateRepository(bool isFromAzureFunction = false)
        {
            if (this.sentNotificationDataRepository == null)
            {
                this.sentNotificationDataRepository = new SentNotificationDataRepository(
                    this.configuration,
                    isFromAzureFunction);
            }

            return this.sentNotificationDataRepository;
        }
    }
}
