// <copyright file="DeliveryState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    /// <summary>
    /// Enuerables indicating the state of a notification per recipient.
    /// </summary>
    public enum DeliveryState
    {
        /// <summary>
        /// Indicating a notification was delivered successfully.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Indicating a notification was failed in sending to a recipient.
        /// </summary>
        Failed,

        /// <summary>
        /// Indicating a notification was throttled.
        /// </summary>
        Throttled,
    }
}
