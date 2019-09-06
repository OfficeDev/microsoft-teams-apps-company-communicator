// <copyright file="SentNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Sent notification entity class.
    /// </summary>
    public class SentNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// Succeeded string.
        /// </summary>
        public static readonly string Succeeded = "Succeeded";

        /// <summary>
        /// Failed string.
        /// </summary>
        public static readonly string Failed = "Failed";

        /// <summary>
        /// Throttled string.
        /// </summary>
        public static readonly string Throttled = "Throttled";

        /// <summary>
        /// Gets or sets Aad Id.
        /// </summary>
        public string AadId { get; set; }

        /// <summary>
        /// Gets or sets the total number of throttle responses.
        /// </summary>
        public int TotalNumberOfThrottles { get; set; }

        /// <summary>
        /// Gets or sets the sent DateTime.
        /// </summary>
        public DateTime SentDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the status code is from the create conversation call.
        /// </summary>
        public bool IsStatusCodeFromCreateConversation { get; set; }

        /// <summary>
        /// Gets or sets the response status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the delivery status
        /// </summary>
        public string DeliveryStatus { get; set; }
    }
}