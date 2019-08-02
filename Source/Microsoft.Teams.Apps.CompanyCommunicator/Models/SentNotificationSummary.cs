// <copyright file="SentNotificationSummary.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System;

    /// <summary>
    /// Sent notification summary model class.
    /// </summary>
    public class SentNotificationSummary
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
        /// Gets or sets Created DateTime value.
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets Sent Date value.
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

        /// <summary>
        /// Gets or sets a value indicating whether the sending process is completed or not.
        /// </summary>
        public bool IsCompleted { get; set; }
    }
}
