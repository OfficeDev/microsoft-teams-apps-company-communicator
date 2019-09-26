// <copyright file="NotificationDataRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository factory service.
    /// </summary>
    public class NotificationDataRepositoryFactory
    {
        private readonly IConfiguration configuration;
        private readonly TableRowKeyGenerator tableRowKeyGenerator;
        private NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDataRepositoryFactory"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public NotificationDataRepositoryFactory(
            IConfiguration configuration,
            TableRowKeyGenerator tableRowKeyGenerator)
        {
            this.configuration = configuration;
            this.tableRowKeyGenerator = tableRowKeyGenerator;
        }

        /// <summary>
        /// Create repository service instance.
        /// </summary>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        /// <returns>It returns a repository service instance.</returns>
        public NotificationDataRepository CreateRepository(bool isFromAzureFunction = false)
        {
            if (this.notificationDataRepository == null)
            {
                this.notificationDataRepository = new NotificationDataRepository(
                this.configuration,
                this.tableRowKeyGenerator,
                isFromAzureFunction);
            }

            return this.notificationDataRepository;
        }
    }
}