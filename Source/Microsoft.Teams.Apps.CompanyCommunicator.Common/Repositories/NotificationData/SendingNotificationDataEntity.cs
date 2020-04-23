// <copyright file="SendingNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Sending notification entity class.
    /// This entity holds the information about the content for a notification
    /// that is either currently being sent or was previously sent.
    /// </summary>
    public class SendingNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the notification id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the content of the notification in serialized JSON form.
        /// </summary>
        public string Content { get; set; }
    }
}
