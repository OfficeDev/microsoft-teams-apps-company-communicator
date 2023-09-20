// <copyright file="CleanUpHistoryRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.CleanUpHistory
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Repository of the CleanUpHistory stored in the table storage.
    /// </summary>
    public class CleanUpHistoryRepository : BaseRepository<CleanUpHistoryEntity>, ICleanUpHistoryRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CleanUpHistoryRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public CleanUpHistoryRepository(
            ILogger<CleanUpHistoryRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions,
            TableRowKeyGenerator tableRowKeyGenerator)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: CleanUpHistoryTableName.TableName,
                  defaultPartitionKey: CleanUpHistoryTableName.DefaultPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
            this.TableRowKeyGenerator = tableRowKeyGenerator;
        }

        /// <inheritdoc/>
        public TableRowKeyGenerator TableRowKeyGenerator { get; set; }

        /// <inheritdoc/>
        public async Task EnsureCleanUpHistoryTableExistsAsync()
        {
            var exists = await this.Table.ExistsAsync();
            if (!exists)
            {
                await this.Table.CreateAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CleanUpHistoryEntity>> GetAllCleanUpHistoryAsync()
        {
            var result = await this.GetAllDeleteAsync();

            return result;
        }
    }
}
