// <copyright file="IUserDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for User Data Repository.
    /// </summary>
    public interface IUserDataRepository : IRepository<UserDataEntity>
    {
        /// <summary>
        /// Get delta link.
        /// </summary>
        /// <returns>Delta link.</returns>
        public Task<string> GetDeltaLinkAsync();

        /// <summary>
        /// Sets delta link.
        /// </summary>
        /// <param name="deltaLink">delta link.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SetDeltaLinkAsync(string deltaLink);
    }
}
