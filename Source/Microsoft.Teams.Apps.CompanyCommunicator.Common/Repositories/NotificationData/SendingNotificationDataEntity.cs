// <copyright file="SendingNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Sending notification entity class.
    /// </summary>
    public class SendingNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the NotificationId value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets Content value.
        /// </summary>
        public string Content { get; set; }
    }
}