// <copyright file="ActiveNotificationEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.ActiveNotification
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Active notification entity class.
    /// </summary>
    public class ActiveNotificationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the NotificationId value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets Content value.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets Token value.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets TokenExpiration value.
        /// </summary>
        public DateTime TokenExpiration { get; set; }
    }
}