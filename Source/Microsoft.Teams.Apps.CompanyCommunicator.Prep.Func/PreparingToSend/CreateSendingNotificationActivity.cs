// <copyright file="CreateSendingNotificationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;

    /// <summary>
    /// This class contains the "create sending notification data entity" durable activity.
    /// This activity creates an entry in the notification data table that holds the
    /// information about the notification that will be used when sending the notification,
    /// namely the serialized JSON content for the notification.
    /// </summary>
    public class CreateSendingNotificationActivity
    {
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSendingNotificationActivity"/> class.
        /// </summary>
        /// <param name="sendingNotificationDataRepository">Sending notification data repository.</param>
        /// <param name="adaptiveCardCreator">The adaptive card creator.</param>
        public CreateSendingNotificationActivity(
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
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            await context.CallActivityWithRetryAsync(
                nameof(CreateSendingNotificationActivity.CreateSendingNotificationAsync),
                ActivitySettings.CommonActivityRetryOptions,
                notificationDataEntity);
        }

        /// <summary>
        /// This method represents the "create sending notification" durable activity.
        /// It creates an entry in the notification data table that holds the
        /// information about the notification that will be used when sending the notification,
        /// namely the serialized JSON content for the notification.
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
                PartitionKey = NotificationDataTableNames.SendingNotificationsPartition,
                RowKey = notificationDataEntity.RowKey,
                NotificationId = notificationDataEntity.RowKey,
                Content = cardString,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
