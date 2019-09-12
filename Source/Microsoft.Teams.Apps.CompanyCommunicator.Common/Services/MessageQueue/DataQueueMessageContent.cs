// <copyright file="DataQueueMessageContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using System;

    /// <summary>
    /// Azure service bus data queue message content class.
    /// </summary>
    public class DataQueueMessageContent
    {
        /// <summary>
        /// Gets or sets the notification id value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the initial send date value.
        /// </summary>
        public DateTime InitialSendDate { get; set; }

        /// <summary>
        /// Gets or sets the total message count.
        /// </summary>
        public int TotalMessageCount { get; set; }
    }
}
