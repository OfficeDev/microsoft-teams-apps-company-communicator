// <copyright file="ChannelDataController.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ChannelData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions;

    /// <summary>
    /// Controller for the Channels data.
    /// </summary>
    [Route("api/channels")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class ChannelDataController : ControllerBase
    {
        private readonly IChannelDataRepository channelDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataController"/> class.
        /// </summary>
        /// <param name="channelDataRepository">Instance of the channel data repository.</param>
        public ChannelDataController(IChannelDataRepository channelDataRepository)
        {
            this.channelDataRepository = channelDataRepository ?? throw new ArgumentNullException(nameof(channelDataRepository));
        }

        /// <summary>
        /// Gets configuration for a specific channel.
        /// </summary>
        /// <param name="channelId">Id of the teams channel.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet("{channelId}")]
        public async Task<ChannelData> GetChannelConfig(string channelId)
        {
            var entity = await this.channelDataRepository.GetChannelConfigByChannelId(channelId);

            var tmpEntity = new ChannelData
            {
                ChannelId = entity.ChannelId,
                ChannelImage = entity.ChannelImage,
                ChannelTitle = entity.ChannelTitle,
            };

            return tmpEntity;
        }

        /// <summary>
        /// Update an channel config.
        /// </summary>
        /// <param name="channeltoupdate">A channel to be updated.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateChannelConfig([FromBody] ChannelData channeltoupdate)
        {
            if (channeltoupdate == null)
            {
                throw new ArgumentNullException(nameof(channeltoupdate));
            }

            await this.channelDataRepository.CreateorUpdateChannelConfig(channeltoupdate);
            return this.Ok();

        }
    }
}