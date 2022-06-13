// <copyright file="GroupAssociationData.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Group Association data model class.
    /// </summary>
    public class GroupAssociationData
    {
        /// <summary>
        /// Gets or sets group Id.
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets email.
        /// </summary>
        public string GroupEmail { get; set; }

        /// <summary>
        /// Gets or sets channelId.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets rowkey.
        /// </summary>
        public string RowKey { get; set; }
    }
}