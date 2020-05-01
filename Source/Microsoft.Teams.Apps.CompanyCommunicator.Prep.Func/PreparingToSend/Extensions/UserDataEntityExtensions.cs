// <copyright file="UserDataEntityExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Extension methods for the UserDataEntity class.
    /// </summary>
    public static class UserDataEntityExtensions
    {
        /// <summary>
        /// Creates a SentNotificationDataEntity in an initialized state from the given UserDataEntity
        /// and partition key.
        /// Makes sure to set the correct recipient type for having been created from a UserDataEntity.
        /// </summary>
        /// <param name="userDataEntity">The user data entity.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>The sent notification data entity.</returns>
        public static SentNotificationDataEntity CreateInitialSentNotificationDataEntity(
            this UserDataEntity userDataEntity,
            string partitionKey)
        {
            return new SentNotificationDataEntity
            {
                PartitionKey = partitionKey,
                RowKey = userDataEntity.AadId,
                RecipientType = SentNotificationDataEntity.UserRecipientType,
                RecipientId = userDataEntity.AadId,
                StatusCode = SentNotificationDataEntity.InitializationStatusCode,
                ConversationId = userDataEntity.ConversationId,
                TenantId = userDataEntity.TenantId,
                UserId = userDataEntity.UserId,
                ServiceUrl = userDataEntity.ServiceUrl,
            };
        }
    }
}
