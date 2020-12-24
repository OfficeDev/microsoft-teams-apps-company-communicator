// <copyright file="ITeamMembersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Team Members service interface.
    /// </summary>
    public interface ITeamMembersService
    {
        /// <summary>
        /// Get all the members in a team using user app id.
        /// </summary>
        /// <param name="teamId">Team Id. Example: "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="serviceUrl">Service url.</param>
        /// <returns>All the members in a team.</returns>
        public Task<IEnumerable<UserDataEntity>> GetUsersAsync(string teamId, string tenantId, string serviceUrl);

        /// <summary>
        /// Get all the members in a team using author app id.
        /// </summary>
        /// <param name="teamId">Team Id. Example: "19:44777361677b439281a0f0cd914cb149@thread.skype".</param>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="serviceUrl">Service url.</param>
        /// <returns>All the members in a team.</returns>
        public Task<IEnumerable<UserDataEntity>> GetAuthorsAsync(string teamId, string tenantId, string serviceUrl);
    }
}
