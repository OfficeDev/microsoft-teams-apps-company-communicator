// <copyright file="SendingNotificationDataRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository factory service.
    /// </summary>
    public class SendingNotificationDataRepositoryFactory
    {
        private readonly IConfiguration configuration;
        private SendingNotificationDataRepository sendingNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendingNotificationDataRepositoryFactory"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public SendingNotificationDataRepositoryFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Create repository service instance.
        /// </summary>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        /// <returns>It returns a repository service instance.</returns>
        public SendingNotificationDataRepository CreateRepository(bool isFromAzureFunction = false)
        {
            if (this.sendingNotificationDataRepository == null)
            {
                this.sendingNotificationDataRepository = new SendingNotificationDataRepository(
                    this.configuration,
                    isFromAzureFunction);
            }

            return this.sendingNotificationDataRepository;
        }
    }
}
