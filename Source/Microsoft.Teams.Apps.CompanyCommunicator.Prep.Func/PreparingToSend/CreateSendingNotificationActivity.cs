// <copyright file="CreateSendingNotificationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// This class contains the "create sending notification data entity" durable activity.
    /// </summary>
    public class CreateSendingNotificationActivity
    {
        private readonly SendingNotificationDataRepositoryFactory sendingNotificationDataRepositoryFactory;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSendingNotificationActivity"/> class.
        /// </summary>
        /// <param name="sendingNotificationDataRepositoryFactory">Sending notification data repository factory.</param>
        /// <param name="adaptiveCardCreator">The adaptive card creator.</param>
        public CreateSendingNotificationActivity(
            SendingNotificationDataRepositoryFactory sendingNotificationDataRepositoryFactory,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.sendingNotificationDataRepositoryFactory = sendingNotificationDataRepositoryFactory;
            this.adaptiveCardCreator = adaptiveCardCreator;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            await context.CallActivityWithRetryAsync(
                nameof(CreateSendingNotificationActivity.CreateSendingNotificationAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
                notificationDataEntity);
        }

        /// <summary>
        /// This method represents the "create sending notification" durable activity.
        /// </summary>
        /// <param name="notificationDataEntity">A notification to be sent to recipients.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(CreateSendingNotificationAsync))]
        public async Task CreateSendingNotificationAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var cardString = this.adaptiveCardCreator.CreateAdaptiveCard(notificationDataEntity).ToJson();

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                RowKey = notificationDataEntity.RowKey,
                NotificationId = notificationDataEntity.RowKey,
                Content = cardString,
            };

            await this.sendingNotificationDataRepositoryFactory.CreateRepository(true).CreateOrUpdateAsync(sendingNotification);
        }
    }
}
