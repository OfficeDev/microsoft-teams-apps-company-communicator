// <copyright file="SentNotificationStatusDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Sent notification status data entity class.
    /// </summary>
    public class SentNotificationStatusDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the sent DateTime.
        /// </summary>
        public DateTime SentDate { get; set; }

        /// <summary>
        /// Gets or sets the response status code.
        /// </summary>
        public int StatusCode { get; set; }
    }
}