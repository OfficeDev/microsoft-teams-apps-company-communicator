// <copyright file="SentNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Sent notification model class.
    /// </summary>
    public class SentNotification : BaseNotification
    {
        /// <summary>
        /// Gets or sets the Sent Date value.
        /// </summary>
        public string SentDate { get; set; }

        /// <summary>
        /// Gets or sets the number of recipients who have received the notification successfully.
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the number of recipients who failed in receiving the notification.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the number of recipients who were throttled out.
        /// </summary>
        public int Throttled { get; set; }
    }
}
