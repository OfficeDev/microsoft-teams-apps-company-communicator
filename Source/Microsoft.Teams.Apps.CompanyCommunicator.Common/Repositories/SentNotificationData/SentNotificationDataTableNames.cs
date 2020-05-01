// <copyright file="SentNotificationDataTableNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData
{
    /// <summary>
    /// Sent notification data table names.
    /// </summary>
    public static class SentNotificationDataTableNames
    {
        /// <summary>
        /// Table name for the sent notification data table.
        /// </summary>
        public static readonly string TableName = "SentNotificationData";

        /// <summary>
        /// Default partition - should not be used.
        /// </summary>
        public static readonly string DefaultPartition = "Default";
    }
}
