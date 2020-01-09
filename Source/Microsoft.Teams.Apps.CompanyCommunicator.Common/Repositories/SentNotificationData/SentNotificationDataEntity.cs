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
        /// Continued string - this is a state where sending the current notification resulted
        /// in an exception. Because of this, this string will be stored in the repository
        /// until a more final state is reached and the notification will be attempted to be
        /// sent again.
        /// </summary>
        public static readonly string Continued = "Continued";

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
        public DateTime? SentDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the status code is from the create conversation call.
        /// </summary>
        public bool IsStatusCodeFromCreateConversation { get; set; }

        /// <summary>
        /// Gets or sets the response status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a string representing all of the status code responses for this recipient.
        /// </summary>
        public string AllStatusCodeResults { get; set; }

        /// <summary>
        /// Gets or sets the number of attempts it took to send a notification to this recipient.
        /// </summary>
        public int NumberOfAttemptsToSend { get; set; }

        /// <summary>
        /// Gets or sets the delivery status
        /// </summary>
        public string DeliveryStatus { get; set; }

        /// <summary>
        /// Gets or sets ConversationId.
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets ServiceUrl.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets UserId.
        /// </summary>
        public string UserId { get; set; }
    }
}