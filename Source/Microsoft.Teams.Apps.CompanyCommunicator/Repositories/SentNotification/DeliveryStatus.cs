// <copyright file="DeliveryStatus.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.SentNotification
{
    /// <summary>
    /// Enum indicating a notification's delivery status.
    /// </summary>
    public enum DeliveryStatus
    {
        /// <summary>
        /// Indicates a notification is pending for delivery.
        /// </summary>
        Pending,

        /// <summary>
        /// Indicates a notification was delivered successfully.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Indicates a notification was failed in delivering.
        /// </summary>
        Failed,

        /// <summary>
        /// Indicates a notification was throttled out.
        /// </summary>
        Throttled,
    }
}
