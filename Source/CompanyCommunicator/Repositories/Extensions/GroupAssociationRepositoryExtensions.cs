// <copyright file="GroupAssociationRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.GroupAssociationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Extensions for the repository of the group association data.
    /// </summary>
    public static class GroupAssociationRepositoryExtensions
    {

        /// <summary>
        /// Create a new Group Association based on the GroupAssociation data.
        /// </summary>
        /// <param name="groupAssociationDataRepository">Group association repository.</param>
        /// <param name="draftGroupAssociationData">Group Association data received from the web interface.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task CreateGroupAssociation(
            this IGroupAssociationDataRepository groupAssociationDataRepository,
            GroupAssociationData draftGroupAssociationData)
        {
            var tmpGroupAssociation = new GroupAssociationDataEntity
            {
                PartitionKey = GroupAssociationTableNames.GroupDataPartition,
                // RowKey = draftGroupAssociationData.GroupId,
                RowKey = Guid.NewGuid().ToString(),
                ChannelId = draftGroupAssociationData.ChannelId,
                GroupName = draftGroupAssociationData.GroupName,
                GroupId = draftGroupAssociationData.GroupId,
                Email = draftGroupAssociationData.GroupEmail,
            };

            await groupAssociationDataRepository.CreateOrUpdateAsync(tmpGroupAssociation);
        }
    }
}