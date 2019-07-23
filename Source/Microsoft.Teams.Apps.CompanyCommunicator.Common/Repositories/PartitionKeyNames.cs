// <copyright file="PartitionKeyNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    /// <summary>
    /// Partition key names used in the table storage.
    /// </summary>
    public static class PartitionKeyNames
    {
        /// <summary>
        /// Teams data partition key names.
        /// </summary>
        public static class Metadata
        {
            /// <summary>
            /// Team data partition key name.
            /// </summary>
            public static readonly string TeamData = "TeamData";

            /// <summary>
            /// Users data partition key name.
            /// </summary>
            public static readonly string UserData = "UserData";
        }

        /// <summary>
        /// Notification partition key names.
        /// </summary>
        public static class Notification
        {
            /// <summary>
            /// Draft notifications partition key name.
            /// </summary>
            public static readonly string DraftNotifications = "DraftNotifications";

            /// <summary>
            /// Sent notifications partition key name.
            /// </summary>
            public static readonly string SentNotifications = "SentNotifications";

            /// <summary>
            /// Active notifications partition key name.
            /// </summary>
            public static readonly string ActiveNotifications = "ActiveNotifications";
        }
    }
}
