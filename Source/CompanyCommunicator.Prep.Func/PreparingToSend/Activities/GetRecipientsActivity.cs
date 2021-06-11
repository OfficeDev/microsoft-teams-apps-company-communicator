// <copyright file="GetRecipientsActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// Reads all the recipients from Sent notification table.
    /// </summary>
    public class GetRecipientsActivity
    {
        // Recommended data count size that should be returned from activity function to orchestrator.
        // Please note that increasing this value can cause OutOfMemoryException.
        private const int MaxResultSize = 100000;

        // Maximum record count that Table storage returns.
        private const int UserCount = 1000;
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
        /// Reads all the recipients from Sent notification table.
        /// </summary>
        /// <param name="notification">notification.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.GetRecipientsActivity)]
        public async Task<(IEnumerable<SentNotificationDataEntity>, TableContinuationToken)> GetRecipientsAsync([ActivityTrigger] NotificationDataEntity notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var results = await this.sentNotificationDataRepository.GetPagedAsync(notification.Id, UserCount);
            var recipients = new List<SentNotificationDataEntity>();
            if (results.Item1 != null)
            {
                recipients.AddRange(results.Item1);
            }

            while (results.Item2 != null && recipients.Count < MaxResultSize)
            {
                results = await this.sentNotificationDataRepository.GetPagedAsync(notification.Id, UserCount, results.Item2);
                if (results.Item1 != null)
                {
                    recipients.AddRange(results.Item1);
                }
            }

            return (recipients, results.Item2);
        }

        /// <summary>
        /// Reads all the recipients from Sent notification table.
        /// </summary>
        /// <param name="input">Input containing notification id and continuation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.GetRecipientsByTokenActivity)]
        public async Task<(IEnumerable<SentNotificationDataEntity>, TableContinuationToken)> GetRecipientsByTokenAsync(
            [ActivityTrigger](string notificationId, TableContinuationToken tableContinuationToken) input)
        {
            if (input.notificationId == null)
            {
                throw new ArgumentNullException(nameof(input.notificationId));
            }

            if (input.tableContinuationToken == null)
            {
                throw new ArgumentNullException(nameof(input.tableContinuationToken));
            }

            var recipients = new List<SentNotificationDataEntity>();
            while (input.tableContinuationToken != null && recipients.Count < MaxResultSize)
            {
                var results = await this.sentNotificationDataRepository.GetPagedAsync(input.notificationId, UserCount, input.tableContinuationToken);
                if (results.Item1 != null)
                {
                    recipients.AddRange(results.Item1);
                }

                input.tableContinuationToken = results.Item2;
            }

            return (recipients, input.tableContinuationToken);
        }

        /// <summary>
        /// Reads all the recipients from Sent notification table who do not have conversation details.
        /// </summary>
        /// <param name="notification">notification.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.GetPendingRecipientsActivity)]
        public async Task<IEnumerable<SentNotificationDataEntity>> GetPendingRecipientsAsync([ActivityTrigger] NotificationDataEntity notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var recipients = await this.sentNotificationDataRepository.GetAllAsync(notification.Id);
            return recipients.Where(recipient => string.IsNullOrEmpty(recipient.ConversationId));
        }
    }
}
