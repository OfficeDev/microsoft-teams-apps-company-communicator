// <copyright file="TeamDataEntityExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// Extension methods for the TeamDataEntity class.
    /// </summary>
    public static class TeamDataEntityExtensions
    {
        /// <summary>
        /// Creates a SentNotificationDataEntity in an initialized state from the given TeamDataEntity
        /// and partition key.
        /// Makes sure to set the correct recipient type for having been created from a TeamDataEntity.
        /// </summary>
        /// <param name="teamDataEntity">The team data entity.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>The sent notification data entity.</returns>
        public static SentNotificationDataEntity CreateInitialSentNotificationDataEntity(
            this TeamDataEntity teamDataEntity,
            string partitionKey)
        {
            return new SentNotificationDataEntity
            {
                PartitionKey = partitionKey,
                RowKey = teamDataEntity.TeamId,
                RecipientType = SentNotificationDataEntity.TeamRecipientType,
                RecipientId = teamDataEntity.TeamId,
                StatusCode = SentNotificationDataEntity.InitializationStatusCode,
                ConversationId = teamDataEntity.TeamId,
                TenantId = teamDataEntity.TenantId,
                ServiceUrl = teamDataEntity.ServiceUrl,
            };
        }
    }
}
