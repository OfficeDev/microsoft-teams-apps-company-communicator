// <copyright file="RecipientsInfo.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Recipients
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Recipient information.
    /// </summary>
    public class RecipientsInfo
    {
        /// <summary>
        /// Gets or sets the notification id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the total recipient count of the message.
        /// </summary>
        public int TotalRecipientCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there are pending recipients.
        /// </summary>
        public bool IsPendingRecipient { get; set; }

        /// <summary>
        /// Gets or sets the batch names of the recipients.
        /// </summary>
        public List<string> BatchName { get; set; }
    }
}
