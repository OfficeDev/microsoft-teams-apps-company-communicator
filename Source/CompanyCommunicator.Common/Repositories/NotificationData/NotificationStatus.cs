﻿// <copyright file="NotificationStatus.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    /// <summary>
    /// Notification status.
    /// </summary>
    public enum NotificationStatus
    {
        /// <summary>
        /// Status is Unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// Message is queued to be processed.
        /// All new messages sent are created with this status.
        /// </summary>
        Queued,

        /// <summary>
        /// Syncing recipients.
        /// </summary>
        SyncingRecipients,

        /// <summary>
        /// Installing user app for recipients.
        /// </summary>
        InstallingApp,

        /// <summary>
        /// In process of sending the message.
        /// </summary>
        Sending,

        /// <summary>
        /// Message is sent to recipients. This is an end state.
        /// </summary>
        Sent,

        /// <summary>
        /// Failed to send the message. This is an end state.
        /// </summary>
        Failed,

        /// <summary>
        /// In process of canceling the message.
        /// </summary>
        Canceling,

        /// <summary>
        /// Canceled the message. This is an end state.
        /// </summary>
        Canceled,
    }
}
