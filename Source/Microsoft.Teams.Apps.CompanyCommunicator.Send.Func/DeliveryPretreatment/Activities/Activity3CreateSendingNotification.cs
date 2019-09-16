// <copyright file="Activity3CreateSendingNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// Create sending notification activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class Activity3CreateSendingNotification
    {
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity3CreateSendingNotification"/> class.
        /// </summary>
        /// <param name="sendingNotificationDataRepository">Sending notification data repository.</param>
        /// <param name="adaptiveCardCreator">The adaptive card creator.</param>
        public Activity3CreateSendingNotification(
            SendingNotificationDataRepository sendingNotificationDataRepository,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.adaptiveCardCreator = adaptiveCardCreator;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="newSentNotificationId">New sent notification id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity,
            string newSentNotificationId)
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            draftNotificationEntity.RowKey = newSentNotificationId;
            await context.CallActivityWithRetryAsync(
                nameof(Activity3CreateSendingNotification.CreateSendingNotificationAsync),
                retryOptions,
                draftNotificationEntity);

            context.SetCustomStatus(nameof(Activity3CreateSendingNotification.CreateSendingNotificationAsync));
        }

        /// <summary>
        /// Generate an adaptive card in json.
        /// </summary>
        /// <param name="draftNotificationEntity">The draft notification to be sent to audiences.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(CreateSendingNotificationAsync))]
        public async Task CreateSendingNotificationAsync(
            [ActivityTrigger] NotificationDataEntity draftNotificationEntity)
        {
            var cardString = this.adaptiveCardCreator.CreateAdaptiveCard(draftNotificationEntity).ToJson();

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                RowKey = draftNotificationEntity.RowKey,
                NotificationId = draftNotificationEntity.RowKey,
                Content = cardString,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
