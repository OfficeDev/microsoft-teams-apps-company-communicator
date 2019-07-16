// <copyright file="Team.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Team model class.
    /// </summary>
    public class Team
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
