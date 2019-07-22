// <copyright file="DraftNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Draft notification model class.
    /// </summary>
    public class DraftNotification : BaseNotification
    {
        /// <summary>
        /// Gets or sets Teams audience id collection.
        /// </summary>
        public IEnumerable<string> Teams { get; set; }

        /// <summary>
        /// Gets or sets Rosters audience id collection.
        /// </summary>
        public IEnumerable<string> Rosters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a notification should be sent to all the users.
        /// </summary>
        public bool AllUsers { get; set; }
    }
}
