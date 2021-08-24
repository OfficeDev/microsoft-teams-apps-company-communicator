﻿// <copyright file="GetRecipientsActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// Reads all the recipients from Sent notification table.
    /// </summary>
    public class GetRecipientsActivity
    {
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientsActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public GetRecipientsActivity(ISentNotificationDataRepository sentNotificationDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
        }

        /// <summary>
        /// Reads all the batched recipients from Sent notification table.
        /// </summary>
        /// <param name="notificationBatchKey">notification batch key.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.GetRecipientsActivity)]
        public async Task<IEnumerable<SentNotificationDataEntity>> GetRecipientsAsync([ActivityTrigger] string notificationBatchKey)
        {
            _ = notificationBatchKey ?? throw new ArgumentNullException(nameof(notificationBatchKey));

            return await this.sentNotificationDataRepository.GetAllAsync(notificationBatchKey);
        }

        /// <summary>
        /// Reads all the batched recipients from Sent notification table who do not have conversation details.
        /// </summary>
        /// <param name="notificationBatchKey">notification batch key.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.GetPendingRecipientsActivity)]
        public async Task<IEnumerable<SentNotificationDataEntity>> GetPendingRecipientsAsync([ActivityTrigger] string notificationBatchKey)
        {
            _ = notificationBatchKey ?? throw new ArgumentNullException(nameof(notificationBatchKey));

            var recipients = await this.sentNotificationDataRepository.GetAllAsync(notificationBatchKey);
            return recipients.Where(recipient => string.IsNullOrEmpty(recipient.ConversationId));
        }
    }
}
