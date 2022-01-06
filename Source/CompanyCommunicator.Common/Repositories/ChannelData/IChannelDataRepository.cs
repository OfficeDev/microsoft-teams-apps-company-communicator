// <copyright file="IChannelDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ChannelData
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for Channel Data Repository.
    /// </summary>
    public interface IChannelDataRepository : IRepository<ChannelDataEntity>
    {
        /// <summary>
        /// Gets the channel configuration.
        /// </summary>
        /// <param name="channelId">The channel Id of the channel.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<ChannelDataEntity> GetChannelConfigByChannelId(string channelId);

        /// <summary>
        /// Sets the channel config.
        /// </summary>
        /// <param name="channeltoUpdate">Data entity to set/create</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task SetChannelConfig(ChannelDataEntity channeltoUpdate);
    }
}