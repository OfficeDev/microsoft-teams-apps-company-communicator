// <copyright file="UserDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// User data entity class.
    /// </summary>
    public class UserDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the user's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the email address for the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's UPN.
        /// </summary>
        public string Upn { get; set; }

        /// <summary>
        /// Gets or sets the AAD id of the user.
        /// </summary>
        public string AadId { get; set; }

        /// <summary>
        /// Gets or sets the user id for the user as known to the
        /// bot - typically this starts with "29:".
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the conversation id for the chat between the
        /// user and the bot.
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the service URL that can be used by the bot
        /// to send this user a notification.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the tenant id for the user.
        /// </summary>
        public string TenantId { get; set; }
    }
}
