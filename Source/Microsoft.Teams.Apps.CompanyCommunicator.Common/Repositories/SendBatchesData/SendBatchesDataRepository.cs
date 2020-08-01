// <copyright file="SendBatchesDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SendBatchesData
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
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
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public SendBatchesDataRepository(
            ILogger<SendBatchesDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: SendBatchesDataTableNames.TableName,
                  defaultPartitionKey: SendBatchesDataTableNames.DefaultPartition,
                  isItExpectedThatTableAlreadyExists: repositoryOptions.Value.IsItExpectedThatTableAlreadyExists)
        {
        }

        /// <summary>
        /// This method ensures the SendBatchesData table is created in the storage.
        /// This method should be called before kicking off an Azure function that uses the SendBatchesData table.
        /// Otherwise the app will crash.
        /// By design, Azure functions (in this app) do not create a table if it's absent.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task EnsureSendBatchesDataTableExistsAsync()
        {
            var exists = await this.Table.ExistsAsync();
            if (!exists)
            {
                await this.Table.CreateAsync();
            }
        }
    }
}
