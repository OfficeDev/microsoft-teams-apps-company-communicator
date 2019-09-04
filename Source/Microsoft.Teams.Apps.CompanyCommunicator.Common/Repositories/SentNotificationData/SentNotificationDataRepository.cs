// <copyright file="SentNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository of the notification data in the table storage.
    /// </summary>
    public class SentNotificationDataRepository : BaseRepository<SentNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public SentNotificationDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                configuration,
                PartitionKeyNames.SentNotificationDataTable.TableName,
                PartitionKeyNames.SentNotificationDataTable.DefaultPartition,
                isFromAzureFunction)
        {
        }
    }
}