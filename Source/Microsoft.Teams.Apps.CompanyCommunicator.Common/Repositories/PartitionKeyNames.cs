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
        /// Users data table partition key names.
        /// </summary>
        public static class UserDataTable
        {
            /// <summary>
            /// Table name for user data table
            /// </summary>
            public static readonly string TableName = "UserData";

            /// <summary>
            /// Users data partition key name.
            /// </summary>
            public static readonly string UserDataPartition = "UserData";
        }

        /// <summary>
        /// Teams data table partition key names.
        /// </summary>
        public static class TeamDataTable
        {
            /// <summary>
            /// Table name for team data table
            /// </summary>
            public static readonly string TableName = "TeamData";

            /// <summary>
            /// Team data partition key name.
            /// </summary>
            public static readonly string TeamDataPartition = "TeamData";
        }

        /// <summary>
        /// Notification data table partition key names.
        /// </summary>
        public static class NotificationDataTable
        {
            /// <summary>
            /// Table name for notification data table
            /// </summary>
            public static readonly string TableName = "NotificationData";

            /// <summary>
            /// Draft notifications partition key name.
            /// </summary>
            public static readonly string DraftNotificationsPartition = "DraftNotifications";

            /// <summary>
            /// Sending notifications partition key name.
            /// </summary>
            public static readonly string SendingNotificationsPartition = "SendingNotifications";

            /// <summary>
            /// Global sending notification data partition key name.
            /// </summary>
            public static readonly string GlobalSendingNotificationDataPartition = "GlobalSendingNotificationData";

            /// <summary>
            /// Sent notifications partition key name.
            /// </summary>
            public static readonly string SentNotificationsPartition = "SentNotifications";
        }

        /// <summary>
        /// Sent notification data table partition key names.
        /// </summary>
        public static class SentNotificationDataTable
        {
            /// <summary>
            /// Table name for sent notification data table
            /// </summary>
            public static readonly string TableName = "SentNotificationData";

            /// <summary>
            /// Default partion - should not be used.
            /// </summary>
            public static readonly string DefaultPartition = "Default";
        }
    }
}
