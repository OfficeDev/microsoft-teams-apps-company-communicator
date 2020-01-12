// <copyright file="SendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository for the sending notification data in the table storage.
    /// </summary>
    public class SendingNotificationDataRepository : BaseRepository<SendingNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendingNotificationDataRepository"/> class.
        /// </summary>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SendingNotificationDataRepository(IOptions<RepositoryOptions> repositoryOptions)
            : base(
                storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                tableName: PartitionKeyNames.NotificationDataTable.TableName,
                defaultPartitionKey: PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                isExpectedTableAlreadyExist: repositoryOptions.Value.IsExpectedTableAlreadyExist)
        {
        }
    }
}