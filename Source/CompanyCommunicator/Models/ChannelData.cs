// <copyright file="ChannelData.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Group Association data model class.
    /// </summary>
    public class ChannelData
    {
        /// <summary>
        /// Gets or sets the channel id to where the group is associated.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the title to be used on cards for this channel.
        /// </summary>
        public string ChannelTitle { get; set; }

        /// <summary>
        /// Gets or sets the image to be used on cards for this channel.
        /// </summary>
        public string ChannelImage { get; set; }
    }
}