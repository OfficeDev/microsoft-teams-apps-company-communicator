// <copyright file="ExportMessageQueueContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model
{
    /// <summary>
    /// Azure service bus export queue message content class.
    /// </summary>
    public class ExportMessageQueueContent
    {
        /// <summary>
        /// Gets or sets the notification id value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the user id value.
        /// </summary>
        public string UserId { get; set; }
    }
}
