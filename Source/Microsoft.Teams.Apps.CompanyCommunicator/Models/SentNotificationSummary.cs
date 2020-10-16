// <copyright file="SentNotificationSummary.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

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
        /// Gets or sets Sent DateTime value.
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
        /// Gets or sets the total number of messages to be sent.
        /// </summary>
        public int TotalMessageCount { get; set; }

        /// <summary>
        /// Gets or sets the sending started date time.
        /// </summary>
        public DateTime? SendingStartedDate { get; set; }

        /// <summary>
        /// Gets or sets notification status. <see cref="NotificationStatus"/> for possible values.
        /// </summary>
        public string Status { get; set; }
    }
}
