// <copyright file="Notification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System;

    /// <summary>
    /// Message model class.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Gets or sets Notification Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Title value.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets Date value.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets Recipients value.
        /// </summary>
        public string Recipients { get; set; }

        /// <summary>
        /// Gets or sets Acknowledgements value.
        /// </summary>
        public string Acknowledgements { get; set; }

        /// <summary>
        /// Gets or sets Reactions value.
        /// </summary>
        public string Reactions { get; set; }

        /// <summary>
        /// Gets or sets Responses value.
        /// </summary>
        public string Responses { get; set; }
    }
}
