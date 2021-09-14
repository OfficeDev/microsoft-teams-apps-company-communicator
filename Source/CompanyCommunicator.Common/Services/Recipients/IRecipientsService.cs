// <copyright file="IRecipientsService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// Recipient service.
    /// </summary>
    public interface IRecipientsService
    {
        /// <summary>
        /// Batch the list of recipients.
        /// </summary>
        /// <param name="recipients">list of recipients.</param>
        /// <returns>recipients information.</returns>
        Task<RecipientsInfo> BatchRecipients(IEnumerable<SentNotificationDataEntity> recipients);
    }
}
