// <copyright file="MessageDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.NotificationDelivery
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.User;

    /// <summary>
    /// DTO class for the objects stored in the message queue.
    /// </summary>
    public class MessageDTO
    {
        /// <summary>
        /// Gets or sets Notification Id value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets User Data Entity value.
        /// </summary>
        public UserDataEntity UserDataEntity { get; set; }
    }
}