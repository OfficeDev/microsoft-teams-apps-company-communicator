// <copyright file="SentNotificationStatusDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository of the sent notification status data in the table storage.
    /// </summary>
    public class SentNotificationStatusDataRepository : BaseRepository<SentNotificationStatusDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentNotificationStatusDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public SentNotificationStatusDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                configuration,
                PartitionKeyNames.SentNotificationDataTable.TableName,
                PartitionKeyNames.SentNotificationDataTable.DefaultPartition,
                isFromAzureFunction)
        {
        }
    }
}