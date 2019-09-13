// <copyright file="MoveDraftToSentNotificationPartitionActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Move a notification from draft to sent partition activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class MoveDraftToSentNotificationPartitionActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDraftToSentNotificationPartitionActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification repository service.</param>
        public MoveDraftToSentNotificationPartitionActivity(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Generate an adaptive card in json.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>It returns the sent notification's id.</returns>
        [FunctionName(nameof(MoveDraftToSentNotificationPartitionAsync))]
        public async Task<string> MoveDraftToSentNotificationPartitionAsync(
            [ActivityTrigger] MoveDraftToSentNotificationPartitionActivityDTO input)
        {
            input.DraftNotificationEntity.TotalMessageCount = input.TotalAudienceCount;

            var newSentNotificationId =
                await this.notificationDataRepository.MoveDraftToSentPartitionAsync(
                    input.DraftNotificationEntity);

            return newSentNotificationId;
        }
    }
}
