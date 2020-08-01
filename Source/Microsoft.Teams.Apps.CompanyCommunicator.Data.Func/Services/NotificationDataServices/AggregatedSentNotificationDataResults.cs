// <copyright file="AggregatedSentNotificationDataResults.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// An aggregation of the currently known results for a sent notification.
    /// </summary>
    public class AggregatedSentNotificationDataResults
    {
        /// <summary>
        /// Gets or sets the total currently known count of notification results.
        /// </summary>
        public int CurrentTotalNotificationCount { get; set; }

        /// <summary>
        /// Gets or sets the currently known count of successfully sent notifications.
        /// </summary>
        public int SucceededCount { get; set; }

        /// <summary>
        /// Gets or sets the currently known count of notifications that failed to send.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Gets or sets the currently known count of notifications that were throttled.
        /// </summary>
        public int ThrottledCount { get; set; }

        /// <summary>
        /// Gets or sets the currently known count of notifications with recipient not found issue.
        /// </summary>
        public int RecipientNotFoundCount { get; set; }

        /// <summary>
        /// Gets or sets the sent date of the last known notification.
        /// </summary>
        public DateTime? LastSentDate { get; set; }

        /// <summary>
        /// Update the aggregated results with the given sent notification data.
        /// </summary>
        /// <param name="sentNotification">The sent notification data entity.</param>
        public void UpdateAggregatedResults(SentNotificationDataEntity sentNotification)
        {
            this.CurrentTotalNotificationCount++;

            if (sentNotification.DeliveryStatus == SentNotificationDataEntity.Succeeded)
            {
                this.SucceededCount++;
            }
            else if (sentNotification.DeliveryStatus == SentNotificationDataEntity.Failed)
            {
                this.FailedCount++;
            }
            else if (sentNotification.DeliveryStatus == SentNotificationDataEntity.Throttled)
            {
                this.ThrottledCount++;
            }
            else if (sentNotification.DeliveryStatus == SentNotificationDataEntity.RecipientNotFound)
            {
                this.RecipientNotFoundCount++;
            }

            if (sentNotification.SentDate != null
                && (this.LastSentDate == null
                || this.LastSentDate < sentNotification.SentDate))
            {
                this.LastSentDate = sentNotification.SentDate;
            }
        }
    }
}
