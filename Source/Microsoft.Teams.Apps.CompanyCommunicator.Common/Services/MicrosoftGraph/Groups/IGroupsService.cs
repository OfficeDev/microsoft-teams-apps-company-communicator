// <copyright file="IGroupsService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Interface for Groups Service.
    /// </summary>
    public interface IGroupsService
    {
        /// <summary>
        /// get the group by ids.
        /// </summary>
        /// <param name="groupIds">list of group ids.</param>
        /// <returns>list of groups.</returns>
        IAsyncEnumerable<Group> GetByIdsAsync(IEnumerable<string> groupIds);

        /// <summary>
        /// check if list has hidden membership group.
        /// </summary>
        /// <param name="groupIds">list of group ids.</param>
        /// <returns>boolean.</returns>
        Task<bool> ContainsHiddenMembershipAsync(IEnumerable<string> groupIds);

        /// <summary>
        /// Search groups based on query.
        /// </summary>
        /// <param name="query">query param.</param>
        /// <returns>list of group.</returns>
        Task<IList<Group>> SearchAsync(string query);
    }
}
