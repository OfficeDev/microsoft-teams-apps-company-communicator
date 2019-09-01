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
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public SendingNotificationDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                  configuration,
                  PartitionKeyNames.NotificationDataTable.TableName,
                  PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                  isFromAzureFunction)
        {
        }
    }
}