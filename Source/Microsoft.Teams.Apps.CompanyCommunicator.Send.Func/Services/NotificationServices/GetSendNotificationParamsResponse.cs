// <copyright file="GetSendNotificationParamsResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    /// <summary>
    /// The class to hold the necessary parameters generated/fetched for sending the notification.
    /// </summary>
    public class GetSendNotificationParamsResponse
    {
        /// <summary>
        /// Gets or sets the notification content.
        /// </summary>
        public string NotificationContent { get; set; }

        /// <summary>
        /// Gets or sets the recipient's unique identifier.
        ///     If the recipient is a user, this should be the AAD Id.
        ///     If the recipient is a team, this should be the team Id.
        /// </summary>
        public string RecipientId { get; set; }

        /// <summary>
        /// Gets or sets the service URL.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the conversation Id.
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Azure Function should
        /// stop processing the message because something negative occurred
        /// while generating the parameters e.g. getting throttled, a failure, etc.
        /// </summary>
        public bool ForceCloseAzureFunction { get; set; }
    }
}
