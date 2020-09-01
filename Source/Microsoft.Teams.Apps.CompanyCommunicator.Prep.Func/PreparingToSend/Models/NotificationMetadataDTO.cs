// <copyright file="NotificationMetadataDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Class for transferring notification metadata.
    /// </summary>
    public class NotificationMetadataDTO
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
