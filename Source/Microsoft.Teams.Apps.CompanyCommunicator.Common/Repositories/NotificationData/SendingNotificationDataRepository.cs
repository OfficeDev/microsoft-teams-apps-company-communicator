// <copyright file="SendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository for the sending notification data in the table storage.
    /// </summary>
    public class SendingNotificationDataRepository : BaseRepository<SendingNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendingNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SendingNotificationDataRepository(IConfiguration configuration, RepositoryOptions repositoryOptions)
            : base(
                  configuration,
                  PartitionKeyNames.NotificationDataTable.TableName,
                  PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                  repositoryOptions.IsAzureFunction)
        {
        }
    }
}