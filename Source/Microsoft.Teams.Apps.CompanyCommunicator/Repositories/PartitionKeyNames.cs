// <copyright file="PartitionKeyNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    /// <summary>
    /// Partition key names used in the table storage.
    /// </summary>
    public static class PartitionKeyNames
    {
        /// <summary>
        /// Teams data partition key name.
        /// </summary>
        public static readonly string TeamsData = "TeamsData";

        /// <summary>
        /// Users data partition key name.
        /// </summary>
        public static readonly string UserData = "UserData";

        /// <summary>
        /// Bot conversation reference partition key name.
        /// </summary>
        public static readonly string BotConversationReference = "BotConversationReference";

        /// <summary>
        /// Notification partition key name.
        /// </summary>
        public static readonly string Notification = "Notification";
    }
}
