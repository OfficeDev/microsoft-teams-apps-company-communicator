// <copyright file="IExportDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData
{
    using System.Threading.Tasks;

    /// <summary>
    /// interface for export data Repository.
    /// </summary>
    public interface IExportDataRepository : IRepository<ExportDataEntity>
    {
        /// <summary>
        /// This method ensures the ExportData table is created in the storage.
        /// This method should be called before kicking off an Azure function that uses the ExportData table.
        /// Otherwise the app will crash.
        /// By design, Azure functions (in this app) do not create a table if it's absent.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task EnsureExportDataTableExistsAsync();
    }
}