// <copyright file="RecipientsActivity.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;

    /// <summary>
    /// Reads all the recipients from Sent notification table.
    /// </summary>
    public class RecipientsActivity
    {
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IRecipientsService recipientsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientsActivity"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        /// <param name="recipientsService">The recipients service.</param>
        public RecipientsActivity(
            ISentNotificationDataRepository sentNotificationDataRepository,
            IRecipientsService recipientsService)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.recipientsService = recipientsService ?? throw new ArgumentNullException(nameof(recipientsService));
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

        /// <summary>
        /// Batch all the recipient from Sent notification table.
        /// </summary>
        /// <param name="notificationId">notification id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.BatchRecipientsActivity)]
        public async Task<RecipientsInfo> BatchRecipientsAsync([ActivityTrigger] string notificationId)
        {
            _ = notificationId ?? throw new ArgumentNullException(nameof(notificationId));
            var recipients = await this.sentNotificationDataRepository.GetAllAsync(notificationId);
            return await this.recipientsService.BatchRecipients(recipients);
        }
    }
}
