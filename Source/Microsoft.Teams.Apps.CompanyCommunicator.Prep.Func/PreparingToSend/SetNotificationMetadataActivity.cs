// <copyright file="SetNotificationMetadataActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Sets the notification entity's metadata:
    ///     IsPreparingToSend flag to false - in order to indicate that
    ///         the notification is no longer being prepared to be sent.
    ///     TotalMessageCount - in order for the system to know the
    ///         expected number of notifications to be sent.
    /// </summary>
    public class SetNotificationMetadataActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetNotificationMetadataActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">The notification data repository.</param>
        public SetNotificationMetadataActivity(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationId">The notification Id.</param>
        /// <param name="totalNumberOfRecipients">The total number of recipients.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            string notificationId,
            int totalNumberOfRecipients)
        {
            await context.CallActivityWithRetryAsync(
                nameof(SetNotificationMetadataActivity.SetNotificationMetadataAsync),
                ActivitySettings.CommonActivityRetryOptions,
                new SetNotificationMetadataActivityDTO
                {
                    NotificationId = notificationId,
                    TotalNumberOfRecipients = totalNumberOfRecipients,
                });
        }

        /// <summary>
        /// Sets the notification entity's metadata:
        ///     IsPreparingToSend flag to false - in order to indicate that
        ///         the notification is no longer being prepared to be sent.
        ///     TotalMessageCount - in order for the system to know the
        ///         expected number of notifications to be sent.
        /// </summary>
        /// <param name="input">The trigger DTO.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(SetNotificationMetadataAsync))]
        public async Task SetNotificationMetadataAsync(
            [ActivityTrigger] SetNotificationMetadataActivityDTO input)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                input.NotificationId);

            if (notificationDataEntity != null)
            {
                notificationDataEntity.IsPreparingToSend = false;
                notificationDataEntity.TotalMessageCount = input.TotalNumberOfRecipients;

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }
    }
}
