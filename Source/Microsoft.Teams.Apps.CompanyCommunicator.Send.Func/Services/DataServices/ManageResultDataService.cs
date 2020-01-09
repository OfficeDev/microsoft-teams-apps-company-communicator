// <copyright file="ManageResultDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.DataServices
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// The manage result data service.
    /// </summary>
    public class ManageResultDataService
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageResultDataService"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public ManageResultDataService(SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Processes the notification's result data.
        /// </summary>
        /// <param name="notificationId">The notification ID.</param>
        /// <param name="aadId">The AAD ID.</param>
        /// <param name="totalNumberOfThrottles">The total number of throttled requests.</param>
        /// <param name="isStatusCodeFromCreateConversation">A flag indicating if the status code is from a create conversation request.</param>
        /// <param name="statusCode">The final status code.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ProccessResultDataAsync(
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
