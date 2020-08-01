// <copyright file="GlobalSendingNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Entity that holds metadata for all sending operations.
    /// This data is shared by all sending functions for all notifications.
    /// </summary>
    public class GlobalSendingNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets a DateTime that sending of a notification can be retried/resumed.
        /// This is used to trigger a delay for all notifications if the bot is
        /// currently in a long term throttled state.
        /// After this given time, the sending function will attempt sending again to see
        /// if the bot is still in a throttled state.
        /// </summary>
        public DateTime? SendRetryDelayTime { get; set; }
    }
}
