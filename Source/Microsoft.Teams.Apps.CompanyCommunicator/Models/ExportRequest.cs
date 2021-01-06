// <copyright file="ExportRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Export request model class.
    /// </summary>
    public class ExportRequest
    {
        /// <summary>
        /// Gets or sets the notification id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Team Id.
        /// </summary>
        public string TeamId { get; set; }
    }
}
