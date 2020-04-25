// <copyright file="SetNotificationIsPrepCompleteActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// This activity sets the notification entity's IsPreparingToSend flag to false in order to indicate that
    /// the notification is no longer being prepared to be sent.
    /// </summary>
    public class SetNotificationIsPrepCompleteActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetNotificationIsPrepCompleteActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        public SetNotificationIsPrepCompleteActivity(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationId">The notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            string notificationId)
        {
            await context.CallActivityWithRetryAsync(
                nameof(SetNotificationIsPrepCompleteActivity.SetNotificationIsPreparingToSendAsCompleteAsync),
                ActivitySettings.CommonActivityRetryOptions,
                notificationId);
        }

        /// <summary>
        /// Sets the notification entity's IsPreparingToSend flag to false in order to indicate that
        /// the notification is no longer being prepared to be sent.
        /// </summary>
        /// <param name="notificationId">The notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(SetNotificationIsPreparingToSendAsCompleteAsync))]
        public async Task SetNotificationIsPreparingToSendAsCompleteAsync(
            [ActivityTrigger] string notificationId)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                notificationId);

            if (notificationDataEntity != null)
            {
                notificationDataEntity.IsPreparingToSend = false;

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }
    }
}
