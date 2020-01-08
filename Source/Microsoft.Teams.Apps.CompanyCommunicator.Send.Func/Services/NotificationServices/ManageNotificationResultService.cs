// <copyright file="ManageNotificationResultService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// The manage notification result service.
    /// </summary>
    public class ManageNotificationResultService
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageNotificationResultService"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public ManageNotificationResultService(SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Saves a sent notification piece of data.
        /// </summary>
        /// <param name="notificationId">The notification ID.</param>
        /// <param name="aadId">The AAD ID.</param>
        /// <param name="totalNumberOfThrottles">The total number of throttled requests.</param>
        /// <param name="isStatusCodeFromCreateConversation">A flag indicating if the status code is from a create conversation request.</param>
        /// <param name="statusCode">The final status code.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveSentNotificationDataAsync(
            string notificationId,
            string aadId,
            int totalNumberOfThrottles,
            bool isStatusCodeFromCreateConversation,
            HttpStatusCode statusCode)
        {
            var updatedSentNotificationDataEntity = new SentNotificationDataEntity
            {
                PartitionKey = notificationId,
                RowKey = aadId,
                AadId = aadId,
                TotalNumberOfThrottles = totalNumberOfThrottles,
                SentDate = DateTime.UtcNow,
                IsStatusCodeFromCreateConversation = isStatusCodeFromCreateConversation,
                StatusCode = (int)statusCode,
            };

            if (statusCode == HttpStatusCode.Created)
            {
                updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Succeeded;
            }
            else if (statusCode == HttpStatusCode.TooManyRequests)
            {
                updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Throttled;
            }
            else
            {
                updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Failed;
            }

            await this.sentNotificationDataRepository.InsertOrMergeAsync(updatedSentNotificationDataEntity);
        }
    }
}
