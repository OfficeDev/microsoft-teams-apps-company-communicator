// <copyright file="MessageDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificaitonDelivery
{
    /// <summary>
    /// DTO class for the objects stored in the message queue.
    /// </summary>
    public class MessageDTO
    {
        /// <summary>
        /// Gets or sets the Notification Id value.
        /// </summary>
        public string NotificationId { get; set; }
    }
}