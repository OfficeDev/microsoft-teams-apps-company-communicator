// <copyright file="GroupAssociationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ChannelData
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository of the group association data stored in the table storage.
    /// </summary>
    public class ChannelDataRepository : BaseRepository<ChannelDataEntity>, IChannelDataRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataRepository"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public ChannelDataRepository(
            ILogger<ChannelDataRepository> logger,
            IOptions<RepositoryOptions> repositoryOptions)
            : base(
                logger,
                storageAccountConnectionString: repositoryOptions.Value.StorageAccountConnectionString,
                tableName: ChannelDataTableNames.TableName,
                defaultPartitionKey: ChannelDataTableNames.ChannelDataPartition,
                ensureTableExists: repositoryOptions.Value.EnsureTableExists)
        {
        }

        /// <inheritdoc/>
        public async Task<ChannelDataEntity> GetChannelConfigByChannelId(string channelId)
        {
            var tmpChannel = new ChannelDataEntity
            {
                ChannelId = channelId,
                ChannelImage = string.Empty,
                ChannelTitle = string.Empty,
            };

            var strFilter = TableQuery.GenerateFilterCondition("ChannelId", QueryComparisons.Equal, channelId);

            var result = await this.GetWithFilterAsync(strFilter, ChannelDataTableNames.ChannelDataPartition);

            if (result.Count() > 0)
            {
                return result.First();
            }
            else
            {
                return tmpChannel;
            }
        }

        /// <inheritdoc/>
        public async Task SetChannelConfig(ChannelDataEntity channeltoUpdate)
        {
            if (channeltoUpdate == null)
            {
                throw new ArgumentNullException(nameof(channeltoUpdate));
            }

            await this.CreateOrUpdateAsync(channeltoUpdate);
        }
    }
}