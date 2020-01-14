// <copyright file="DataQueueMessageContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue
{
    using System;

    /// <summary>
    /// Result types for notifications in the data queue.
    /// </summary>
    public enum DataQueueResultType
    {
        /// <summary>
        /// The notification was successfully sent.
        /// </summary>
        Succeeded,

        /// <summary>
        /// The notification was throttled.
        /// </summary>
        Throttled,

        /// <summary>
        /// The notificaiton failed to be sent.
        /// </summary>
        Failed,
    }

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
        /// Gets or sets the sent DateTime of the corresponding notification.
        /// </summary>
        public DateTime? SentDate { get; set; }

        /// <summary>
        /// Gets or sets the result of the corresponding notification.
        /// </summary>
        public DataQueueResultType ResultType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data function should force the
        /// corresponding notification to be complete.
        /// </summary>
        public bool ForceMessageComplete { get; set; }
    }
}
