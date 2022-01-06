// <copyright file="GroupAssociationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.GroupAssociationData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository of the group association data stored in the table storage.
    /// </summary>
    public class GroupAssociationDataRepository : BaseRepository<GroupAssociationDataEntity>, IGroupAssociationDataRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupAssociationDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public GroupAssociationDataRepository(
            ILogger<GroupAssociationDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                logger,
                storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                tableName: GroupAssociationTableNames.TableName,
                defaultPartitionKey: GroupAssociationTableNames.GroupDataPartition,
                ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GroupAssociationDataEntity>> GetGroupAssociationByChannelId(string channelId)
        {
            var strFilter = TableQuery.GenerateFilterCondition("ChannelId", QueryComparisons.Equal, channelId);

            var result = await this.GetWithFilterAsync(strFilter, GroupAssociationTableNames.GroupDataPartition);

            return result;
        }

        /// <inheritdoc/>
        public async Task DeleteGroupAssociationByKey(string key)
        {
            var groupAssociationDataEntity = await this.GetAsync(
                GroupAssociationTableNames.GroupDataPartition, key);

            if (!(groupAssociationDataEntity == null))
            {
                await this.DeleteAsync(groupAssociationDataEntity);
            }
        }
    }
}