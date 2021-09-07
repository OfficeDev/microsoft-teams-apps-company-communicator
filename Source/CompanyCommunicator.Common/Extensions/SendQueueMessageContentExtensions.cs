// <copyright file="SendQueueMessageContentExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;

    /// <summary>
    /// Extension class for <see cref="SendQueueMessageContent"/>.
    /// </summary>
    public static class SendQueueMessageContentExtensions
    {
        /// <summary>
        /// Get service url.
        /// </summary>
        /// <param name="message">Send Queue message.</param>
        /// <returns>Service url.</returns>
        public static string GetServiceUrl(this SendQueueMessageContent message)
        {
            var recipient = message.RecipientData;
            return recipient.RecipientType switch
            {
                RecipientDataType.User => recipient.UserData.ServiceUrl,
                RecipientDataType.Team => recipient.TeamData.ServiceUrl,
                _ => throw new ArgumentException("Invalid recipient type"),
            };
        }

        /// <summary>
        /// Get conversationId.
        /// </summary>
        /// <param name="message">Send Queue message.</param>
        /// <returns>Conversation Id.</returns>
        public static string GetConversationId(this SendQueueMessageContent message)
        {
            var recipient = message.RecipientData;
            return recipient.RecipientType switch
            {
                RecipientDataType.User => recipient.UserData.ConversationId,
                RecipientDataType.Team => recipient.TeamData.TeamId,
                _ => throw new ArgumentException("Invalid recipient type"),
            };
        }

        /// <summary>
        /// Check if recipient guest user.
        /// </summary>
        /// <param name="message">Send Queue message.</param>
        /// <returns>Boolean indicating if it is a guest user.</returns>
        public static bool IsRecipientGuestUser(this SendQueueMessageContent message)
        {
            var recipient = message.RecipientData;
            if (recipient.RecipientType == RecipientDataType.User)
            {
                if (string.IsNullOrEmpty(recipient.UserData.UserType))
                {
                    throw new InvalidOperationException(nameof(recipient.UserData.UserType));
                }
                else if (recipient.UserData.UserType.Equals(UserType.Guest, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
