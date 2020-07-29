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
        /// get list of users by ids.
        /// </summary>
        /// <param name="userIds">list of user ids.</param>
        /// <returns>list of users.</returns>
        Task<IEnumerable<User>> FilterByUserIdsAsync(IEnumerable<string> userIds);

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
    }
}