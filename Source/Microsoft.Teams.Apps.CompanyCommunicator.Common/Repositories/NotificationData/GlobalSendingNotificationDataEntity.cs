// <copyright file="GlobalSendingNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Entity that holds metadata for all sending operations.
    /// </summary>
    public class GlobalSendingNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the Send Retry Delay Sending DateTime value.
        /// </summary>
        public DateTime? SendRetryDelayTime { get; set; }
    }
}
