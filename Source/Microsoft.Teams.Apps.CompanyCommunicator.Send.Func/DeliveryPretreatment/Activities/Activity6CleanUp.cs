// <copyright file="Activity6CleanUp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Send triggers to the Azure send function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class Activity6CleanUp
    {
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity6CleanUp"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification repository service.</param>
        /// <param name="sendingNotificationDataRepository">Sending notification repository service.</param>
        public Activity6CleanUp(
            NotificationDataRepository notificationDataRepository,
            SendingNotificationDataRepository sendingNotificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="newSentNotificationId">New sent notification id.</param>
        /// <param name="ex">Exception.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity,
            string newSentNotificationId,
            Exception ex)
        {
            await context.CallActivityAsync(
                nameof(Activity6CleanUp.CleanUp),
                new Activity6CleanUpDTO
                {
                    DraftNotificationEntity = draftNotificationEntity,
                    NewSentNotificationId = newSentNotificationId,
                    Exception = ex,
                });
        }

        /// <summary>
        /// Send trigger to the Azure send function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(CleanUp))]
        public async Task CleanUp([ActivityTrigger] Activity6CleanUpDTO input)
        {
            await Task.CompletedTask;

            var draftNotificationEntity = input.DraftNotificationEntity;
            var newSentNotificationId = input.NewSentNotificationId;
            var exception = input.Exception;

            var sendingNotificationDataEntity = await this.sendingNotificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition, newSentNotificationId);
            if (sendingNotificationDataEntity != null)
            {
                await this.sendingNotificationDataRepository.DeleteAsync(sendingNotificationDataEntity);
            }

            var sentNotificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                newSentNotificationId);
            if (sentNotificationDataEntity != null)
            {
                await this.notificationDataRepository.DeleteAsync(sentNotificationDataEntity);
            }

            var draftNotificationEntityInStorage = this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.DraftNotificationsPartition,
                draftNotificationEntity.Id);
            if (draftNotificationEntityInStorage == null)
            {
                draftNotificationEntity.IsDraft = true;
                draftNotificationEntity.ExceptionMessage = exception.Message;
                await this.notificationDataRepository.CreateOrUpdateAsync(draftNotificationEntity);
            }
        }
    }
}
