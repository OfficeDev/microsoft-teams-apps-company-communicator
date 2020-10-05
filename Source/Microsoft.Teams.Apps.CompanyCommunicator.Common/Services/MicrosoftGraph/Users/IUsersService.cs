// <copyright file="IUsersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Get the User data.
    /// </summary>
    public interface IUsersService
    {
        /// <summary>
        /// get the list of users by group of userids.
        /// </summary>
        /// <param name="userIdsByGroups">list of grouped user ids.</param>
        /// <returns>list of users.</returns>
        Task<IEnumerable<User>> GetBatchByUserIds(IEnumerable<IEnumerable<string>> userIdsByGroups);

        /// <summary>
        /// get the stream of users.
        /// </summary>
        /// <param name="filter">the filter condition.</param>
        /// <returns>stream of users.</returns>
        IAsyncEnumerable<IEnumerable<User>> GetUsersAsync(string filter = null);

        /// <summary>
        /// get user by id.
        /// </summary>
        /// <param name="userId">the user id.</param>
        /// <returns>user data.</returns>
        Task<User> GetUserAsync(string userId);

        /// <summary>
        /// Gets all the users in the tenant. Doesn't include 'Guest' users.
        ///
        /// Note: If delta link is passed, the API returns delta changes only.
        /// </summary>
        /// <param name="deltaLink">Delta link.</param>
        /// <returns>List of users and delta link.</returns>
        Task<(IEnumerable<User>, string)> GetAllUsersAsync(string deltaLink = null);

        /// <summary>
        /// Checks if the user has teams license.
        /// </summary>
        /// <param name="userId">User's AAD id.</param>
        /// <returns>true if the user has teams license, false otherwise.</returns>
        Task<bool> HasTeamsLicenseAsync(string userId);
    }
}