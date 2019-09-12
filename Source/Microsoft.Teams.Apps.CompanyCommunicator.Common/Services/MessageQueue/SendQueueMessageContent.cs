// <copyright file="SendQueueMessageContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Azure service bus send queue message content class.
    /// </summary>
    public class SendQueueMessageContent
    {
        /// <summary>
        /// Gets or sets the notification id value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the user data entity value
        /// This can be a team.id
        /// </summary>
        public UserDataEntity UserDataEntity { get; set; }
    }
}
