// <copyright file="IGroupAssociationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.GroupAssociationData
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for Group Association Data Repository.
    /// </summary>
    public interface IGroupAssociationDataRepository : IRepository<GroupAssociationDataEntity>
    {
        /// <summary>
        /// Gets group association entities related to a specific channel id.
        /// </summary>
        /// <param name="channelId">Channel Id to filter the groups associated</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<IEnumerable<GroupAssociationDataEntity>> GetGroupAssociationByChannelId(string channelId);


        /// <summary>
        /// Deletes the group association based on the key (group id).
        /// </summary>
        /// <param name="key">Group id to delete.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task DeleteGroupAssociationByKey(string key);
    }
}