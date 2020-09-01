// <copyright file="ExportDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository of the export data stored in the table storage.
    /// </summary>
    public class ExportDataRepository : BaseRepository<ExportDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public ExportDataRepository(
            ILogger<ExportDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                  logger,
                  storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                  tableName: ExportDataTableName.TableName,
                  defaultPartitionKey: ExportDataTableName.DefaultPartition,
                  ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
        }

        /// <summary>
        /// This method ensures the ExportData table is created in the storage.
        /// This method should be called before kicking off an Azure function that uses the ExportData table.
        /// Otherwise the app will crash.
        /// By design, Azure functions (in this app) do not create a table if it's absent.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task EnsureExportDataTableExistsAsync()
        {
            var exists = await this.Table.ExistsAsync();
            if (!exists)
            {
                await this.Table.CreateAsync();
            }
        }
    }
}
