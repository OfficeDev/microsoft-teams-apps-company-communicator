// <copyright file="PrepareToSendQueueMessageContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue
{
    /// <summary>
    /// Azure service bus prepare to send queue message content class.
    /// </summary>
    public class PrepareToSendQueueMessageContent
    {
        /// <summary>
        /// Gets or sets the notification id value.
        /// </summary>
        public string NotificationId { get; set; }
    }
}
