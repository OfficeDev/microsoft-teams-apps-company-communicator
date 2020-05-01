// <copyright file="SetNotificationMetadataActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Class for transferring data to the SetNotificationMetadataActivity.
    /// </summary>
    public class SetNotificationMetadataActivityDTO
    {
        /// <summary>
        /// Gets or sets the notification Id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the total number of recipients.
        /// </summary>
        public int TotalNumberOfRecipients { get; set; }
    }
}
