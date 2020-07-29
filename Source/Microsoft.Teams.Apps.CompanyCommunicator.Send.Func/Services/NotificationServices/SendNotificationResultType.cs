// <copyright file="SendNotificationResultType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    /// <summary>
    /// An enum indicating the different send notification result types.
    /// </summary>
    public enum SendNotificationResultType
    {
        /// <summary>
        /// Type indicating sending the notification succeeded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Type indicating sending the notification was throttled.
        /// </summary>
        Throttled,

        /// <summary>
        /// Type indicating sending the notification failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Type indicating that the recipient can't be found.
        /// When sending a notification to a removed recipient, the send function gets 404 error.
        /// The recipient should be excluded from the list.
        /// </summary>
        RecipientNotFound,
    }
}
