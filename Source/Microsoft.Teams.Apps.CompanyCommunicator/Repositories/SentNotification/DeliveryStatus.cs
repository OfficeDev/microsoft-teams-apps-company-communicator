// <copyright file="DeliveryStatus.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.SentNotification
{
    /// <summary>
    /// Enumerable indicating a notification's delivery status.
    /// </summary>
    public enum DeliveryStatus
    {
        /// <summary>
        /// Indicating a notification is pending for delivery.
        /// </summary>
        Pending,

        /// <summary>
        /// Indicating a notification was delivered successfully.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Indicating a notification was failed in sending to a audience.
        /// </summary>
        Failed,

        /// <summary>
        /// Indicating a notification was throttled.
        /// </summary>
        Throttled,
    }
}
