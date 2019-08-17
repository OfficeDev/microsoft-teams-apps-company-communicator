// <copyright file="TeamDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Teams data entity class.
    /// </summary>
    public class TeamDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets team Id.
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets service Url.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets tenant Id.
        /// </summary>
        public string TenantId { get; set; }
    }
}
