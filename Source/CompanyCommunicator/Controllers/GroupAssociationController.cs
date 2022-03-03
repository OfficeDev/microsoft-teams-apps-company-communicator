// <copyright file="GroupAssociationController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.GroupAssociationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions;

    /// <summary>
    /// Controller for the Groups Association data.
    /// </summary>
    [Route("api/groupassociations")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class GroupAssociationController : ControllerBase
    {
        private readonly IGroupAssociationDataRepository groupAssociationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupAssociationController"/> class.
        /// </summary>
        /// <param name="groupAssociationDataRepository">Instance of the group association repository.</param>
        public GroupAssociationController(IGroupAssociationDataRepository groupAssociationDataRepository)
        {
            this.groupAssociationDataRepository = groupAssociationDataRepository ?? throw new ArgumentNullException(nameof(groupAssociationDataRepository));
        }

        /// <summary>
        /// Gets all groups associates to the channel id.
        /// </summary>
        /// <param name="channelId">Id of the teams channel.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet("{channelId}")]
        public async Task<IEnumerable<GroupAssociationData>> GetAllGroupsbyChannelIdAsync(string channelId)
        {
            var entities = await this.groupAssociationDataRepository.GetGroupAssociationByChannelId(channelId);
            var result = new List<GroupAssociationData>();
            foreach (var entity in entities)
            {
                var group = new GroupAssociationData
                {
                    RowKey = entity.RowKey,
                    GroupId = entity.GroupId,
                    GroupEmail = entity.Email,
                    GroupName = entity.GroupName,
                    ChannelId = entity.ChannelId,
                };
                result.Add(group);
            }

            return result;
        }

        /// <summary>
        /// Creates a new group in the database.
        /// </summary>
        /// <param name="groupData">Object to create.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        public async Task CreateGroupAssociationAsync([FromBody] GroupAssociationData groupData)
        {
            if (groupData == null)
            {
                throw new ArgumentNullException(nameof(groupData));
            }

            await this.groupAssociationDataRepository.CreateGroupAssociation(groupData);
        }

        /// <summary>
        /// Deletes a group association based on its key.
        /// </summary>
        /// <param name="key">Group id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [HttpDelete("{key}")]
        public async Task DeleteGroupAssociationAsync(string key)
        {
            await this.groupAssociationDataRepository.DeleteGroupAssociationByKey(key);
        }
    }
}