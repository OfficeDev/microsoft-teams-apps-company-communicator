// <copyright file="ChannelDataRepositoryExtension.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions
{
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ChannelData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Extensions for the repository of the group association data.
    /// </summary>
    public static class ChannelDataRepositoryExtensions
    {

        /// <summary>
        /// Creates or updates a channel config based on the channelData.
        /// </summary>
        /// <param name="channelDataRepository">Channel Data repository.</param>
        /// <param name="draftChannelData">Channel data received from the web interface.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task CreateorUpdateChannelConfig(
            this IChannelDataRepository channelDataRepository,
            ChannelData draftChannelData)
        {
            var tmpChannel = new ChannelDataEntity
            {
                PartitionKey = ChannelDataTableNames.ChannelDataPartition,
                RowKey = draftChannelData.ChannelId,
                ChannelId = draftChannelData.ChannelId,
                ChannelImage = draftChannelData.ChannelImage,
                ChannelTitle = draftChannelData.ChannelTitle,
            };

            await channelDataRepository.CreateOrUpdateAsync(tmpChannel);
        }
    }
}