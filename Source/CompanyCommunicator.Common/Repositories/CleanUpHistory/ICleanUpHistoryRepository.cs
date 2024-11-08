// <copyright file="ICleanUpHistoryRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.CleanUpHistory
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// interface for export data Repository.
    /// </summary>
    public interface ICleanUpHistoryRepository : IRepository<CleanUpHistoryEntity>
    {
        /// <summary>
        /// Gets or sets table row key generator.
        /// </summary>
        public TableRowKeyGenerator TableRowKeyGenerator { get; set; }

        /// <summary>
        /// This method ensures the CleanUpHistory table is created in the storage.
        /// This method should be called before kicking off an Azure function that uses the CleanUpHistory table.
        /// Otherwise the app will crash.
        /// By design, Azure functions (in this app) do not create a table if it's absent.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task EnsureCleanUpHistoryTableExistsAsync();

        /// <summary>
        /// Get all CleanUp History entities from the table storage.
        /// </summary>
        /// <returns>All entities of Cleanup History.</returns>
        public Task<IEnumerable<CleanUpHistoryEntity>> GetAllCleanUpHistoryAsync();
    }
}
