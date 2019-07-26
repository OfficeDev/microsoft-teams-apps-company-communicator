// <copyright file="ChannelDataDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    /// <summary>
    /// Channel data DTO class.
    /// </summary>
    public class ChannelDataDTO
    {
        /// <summary>
        /// Gets or sets Event Type value.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets Team value.
        /// </summary>
        public TeamDTO Team { get; set; }

        /// <summary>
        /// Gets or sets Tenant value.
        /// </summary>
        public TenantDTO Tenant { get; set; }

        /// <summary>
        /// Team DTO class.
        /// </summary>
        public class TeamDTO
        {
            /// <summary>
            /// Gets or sets Id value.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets Name value.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets AadGroupId value.
            /// </summary>
            public string AadGroupId { get; set; }
        }

        /// <summary>
        /// Tenant DTO class.
        /// </summary>
        public class TenantDTO
        {
            /// <summary>
            /// Gets or sets Id value.
            /// </summary>
            public string Id { get; set; }
        }
    }
}