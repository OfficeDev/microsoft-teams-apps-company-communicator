// <copyright file="SentNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Sent notification entity class.
    /// This entity holds all of the information about a recipient and the results for
    /// a notification having been sent to that recipient.
    /// </summary>
    public class SentNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// This value is to be used when the entity is first initialized and stored and does
        /// not yet have a valid status code from a response for an attempt at sending the
        /// notification to the recipient.
        /// </summary>
        public static readonly int InitializationStatusCode = 0;

        /// <summary>
        /// String indicating the recipient type for the given notification was a user.
        /// </summary>
        public static readonly string UserRecipientType = "User";

        /// <summary>
        /// String indicating the recipient type for the given notification was a team.
        /// </summary>
        public static readonly string TeamRecipientType = "Team";

        /// <summary>
        /// String indicating success of sending the notification to the recipient.
        /// </summary>
        public static readonly string Succeeded = "Succeeded";

        /// <summary>
        /// String indicating a failure response was received when sending the notification to
        /// the recipient.
        /// </summary>
        public static readonly string Failed = "Failed";

        /// <summary>
        /// [Deprecated] String indicationg sending the notification to the recipient was throttled
        /// and not sent successfully.
        /// </summary>
        public static readonly string Throttled = "Throttled";

        /// <summary>
        /// String indicating sending the current notification resulted
        /// in an exception. Because of this, this string will be stored in the repository
        /// until a more final state is reached by attempting to send the notification again.
        /// </summary>
        public static readonly string Continued = "Continued";

        /// <summary>
        /// Gets or sets a value indicating which type of recipient the notification was sent to
        /// using the recipient type strings at the top of this class.
        /// </summary>
        public string RecipientType { get; set; }

        /// <summary>
        /// Gets or sets the recipient's unique identifier.
        /// If the recipient is a user, this should be the AAD id.
        /// If the recipient is a team, this should be the team id.
        /// </summary>
        public string RecipientId { get; set; }

        /// <summary>
        /// Gets or sets the total number of throttle responses the bot received when trying
        /// to send the notification to this recipient.
        /// </summary>
        public int TotalNumberOfThrottles { get; set; }

        /// <summary>
        /// Gets or sets the DateTime the last recorded attempt at sending the notification to this
        /// recipient was completed.
        /// </summary>
        public DateTime? SentDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the status code is from the create conversation call.
        /// </summary>
        public bool IsStatusCodeFromCreateConversation { get; set; }

        /// <summary>
        /// Gets or sets the last recorded response status code received by the bot when attempting to
        /// send the notification to this recipient.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list representing all of the final recorded status code responses the
        /// bot received when sending the notification to this recipient.
        /// Note: this should only ever be one status code unless the recipient incorrectly received multiple
        /// notifications.
        /// </summary>
        public string AllStatusCodeResults { get; set; }

        /// <summary>
        /// Gets or sets the number of attempts the bot made to send a notification to this recipient including
        /// attempts that were throttled and attempts that received a failure status code that was retried.
        /// </summary>
        public int NumberOfAttemptsToSend { get; set; }

        /// <summary>
        /// Gets or sets the summarized delivery status for the notification to this recipient using the
        /// status strings at the top of this class.
        /// </summary>
        public string DeliveryStatus { get; set; }

        /// <summary>
        /// Gets or sets the conversation id for the recipient.
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the service url for the recipient.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the tenant id for the recipient.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the user id for the recipient.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the error message for the last recorded response
        /// received by the bot when the final attempt to send the notification
        /// to this recipient resulted in a failure.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
