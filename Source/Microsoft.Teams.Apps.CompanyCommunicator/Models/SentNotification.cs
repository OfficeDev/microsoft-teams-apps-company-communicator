// <copyright file="SentNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Sent notification model class.
    /// </summary>
    public class SentNotification : BaseNotification
    {
        /// <summary>
        /// Gets or sets the sending started date time.
        /// </summary>
        public DateTime? SendingStartedDate { get; set; }

        /// <summary>
        /// Gets or sets the Sent DateTime value.
        /// </summary>
        public DateTime? SentDate { get; set; }

        /// <summary>
        /// Gets or sets the number of recipients who have received the notification successfully.
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the number of recipients who failed in receiving the notification.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the number of recipients whose delivery status is unknown.
        /// </summary>
        public int? Unknown { get; set; }

        /// <summary>
        /// Gets or sets Teams audience name collection.
        /// </summary>
        public IEnumerable<string> TeamNames { get; set; }

        /// <summary>
        /// Gets or sets Rosters audience name collection.
        /// </summary>
        public IEnumerable<string> RosterNames { get; set; }

        /// <summary>
        /// Gets or sets Groups audience name collection.
        /// </summary>
        public IEnumerable<string> GroupNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a notification was sent to all users.
        /// </summary>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Gets or sets error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets warning message.
        /// </summary>
        public string WarningMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user can download notification.
        /// </summary>
        public bool CanDownload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sending is in progress.
        /// </summary>
        public bool SendingCompleted { get; set; }
    }
}
