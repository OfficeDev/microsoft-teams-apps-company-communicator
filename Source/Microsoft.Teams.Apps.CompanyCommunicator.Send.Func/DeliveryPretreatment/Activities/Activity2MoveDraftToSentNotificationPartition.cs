// <copyright file="Activity2MoveDraftToSentNotificationPartition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Move a notification from draft to sent partition activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class Activity2MoveDraftToSentNotificationPartition
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity2MoveDraftToSentNotificationPartition"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification repository service.</param>
        public Activity2MoveDraftToSentNotificationPartition(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="messageBatchesToBeSent">Message batches to be sent to Azure service bus.</param>
        /// <returns>New sent notification id.</returns>
        public async Task<string> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity,
            List<List<UserDataEntity>> messageBatchesToBeSent)
        {
            var totalMessagesToBeSentToServiceBusCount = messageBatchesToBeSent.SelectMany(p => p).Count();

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            var newSentNotificationId = await context.CallActivityWithRetryAsync<string>(
                nameof(Activity2MoveDraftToSentNotificationPartition.MoveDraftToSentNotificationPartitionAsync),
                retryOptions,
                new Activity2MoveDraftToSentNotificationPartitionDTO
                {
                    DraftNotificationEntity = draftNotificationEntity,
                    TotalMessagesToBeSentToServiceBusCount = totalMessagesToBeSentToServiceBusCount,
                });

            context.SetCustomStatus(nameof(Activity2MoveDraftToSentNotificationPartition.MoveDraftToSentNotificationPartitionAsync));

            return newSentNotificationId;
        }

        /// <summary>
        /// Generate an adaptive card in json.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>It returns the sent notification's id.</returns>
        [FunctionName(nameof(MoveDraftToSentNotificationPartitionAsync))]
        public async Task<string> MoveDraftToSentNotificationPartitionAsync(
            [ActivityTrigger] Activity2MoveDraftToSentNotificationPartitionDTO input)
        {
            input.DraftNotificationEntity.TotalMessageCount = input.TotalMessagesToBeSentToServiceBusCount;

            var newSentNotificationId =
                await this.notificationDataRepository.MoveDraftToSentPartitionAsync(input.DraftNotificationEntity);

            return newSentNotificationId;
        }
    }
}
