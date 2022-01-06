// <copyright file="GroupAssociationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.GroupAssociationData
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Group Association data entity class.
    /// This entity holds the information about a group association with channels in teams.
    /// </summary>
    public class GroupAssociationDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the channel id to where the group is associated.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets the id for the group.
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets the email for the team.
        /// </summary>
        public string Email { get; set; }
    }
}