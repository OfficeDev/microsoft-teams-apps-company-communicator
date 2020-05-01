// <copyright file="RecipientData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Holds the data needed by the sending Azure Function to send a notification to
    /// the provided recipient.
    /// </summary>
    public class RecipientData
    {
        /// <summary>
        /// Gets or sets the type used for indicating to the sending Azure Function what
        /// type of recipient to which the notification is to be delivered.
        /// </summary>
        public RecipientDataType RecipientType { get; set; }

        /// <summary>
        /// Gets or sets the recipient's unique identifier.
        ///     If the recipient is a user, this should be the AAD Id.
        ///     If the recipient is a team, this should be the team Id.
        /// </summary>
        public string RecipientId { get; set; }

        /// <summary>
        /// Gets or sets the user data needed to send a user a notification.
        /// Note: this should be set if the recipient type indicates the recipient is a user.
        /// </summary>
        public UserDataEntity UserData { get; set; }

        /// <summary>
        /// Gets or sets the team data needed to send a team a notification.
        /// Note: this should be set if the recipient type indicates the recipient is a team.
        /// </summary>
        public TeamDataEntity TeamData { get; set; }
    }
}
