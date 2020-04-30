// <copyright file="SendBatchesDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData
{
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// Repository for the send batches data in the table storage.
    /// </summary>
    public class SendBatchesDataRepository : BaseRepository<SentNotificationDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendBatchesDataRepository"/> class.
        /// </summary>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SendBatchesDataRepository(IOptions<RepositoryOptions> repositoryOptions)
            : base(
                storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                tableName: SendBatchesDataTableNames.TableName,
                defaultPartitionKey: SendBatchesDataTableNames.DefaultPartition,
                isItExpectedThatTableAlreadyExists: repositoryOptions.Value.IsItExpectedThatTableAlreadyExists)
        {
        }
    }
}
