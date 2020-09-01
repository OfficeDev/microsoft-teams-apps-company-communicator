// <copyright file="SendMessageDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Send message DTO.
    /// </summary>
    public class SendMessageDTO
    {
        /// <summary>
        /// Gets or sets the notification Id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the total batch count.
        /// </summary>
        public int TotalBatchCount { get; set; }
    }
}
