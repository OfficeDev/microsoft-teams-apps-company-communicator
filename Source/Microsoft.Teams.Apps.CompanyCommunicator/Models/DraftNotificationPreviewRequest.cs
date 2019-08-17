// <copyright file="DraftNotificationPreviewRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Draft notification preview request model class.
    /// </summary>
    public class DraftNotificationPreviewRequest
    {
        /// <summary>
        /// Gets or sets draft notification id.
        /// </summary>
        public string DraftNotificationId { get; set; }

        /// <summary>
        /// Gets or sets Teams team id.
        /// </summary>
        public string TeamsTeamId { get; set; }

        /// <summary>
        /// Gets or sets Teams channel id.
        /// </summary>
        public string TeamsChannelId { get; set; }
    }
}
