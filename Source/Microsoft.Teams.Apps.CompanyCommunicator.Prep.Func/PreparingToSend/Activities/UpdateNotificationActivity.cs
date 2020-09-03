// <copyright file="UpdateNotificationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Update notification entity's metadata:
    ///     IsPreparingToSend flag to false - in order to indicate that
    ///         the notification is no longer being prepared to be sent.
    ///     TotalMessageCount - in order for the system to know the
    ///         expected number of notifications to be sent.
    /// </summary>
    public class UpdateNotificationActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateNotificationActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        public UpdateNotificationActivity(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
        }

        /// <summary>
        /// Sets the notification entity's metadata:
        ///     IsPreparingToSend flag to false - in order to indicate that
        ///         the notification is no longer being prepared to be sent.
        ///     TotalMessageCount - in order for the system to know the
        ///         expected number of notifications to be sent.
        /// </summary>
        /// <param name="input">The trigger DTO.</param>
        /// <param name="log">logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.UpdateNotificationActivity)]
        public async Task RunAsync(
            [ActivityTrigger] NotificationMetadataDTO input,
            ILogger log)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                input.NotificationId);

            if (notificationDataEntity == null)
            {
                log.LogError($"Notification entity not found. Notification Id: {input.NotificationId}");
                return;
            }

            notificationDataEntity.IsPreparingToSend = false;
            notificationDataEntity.TotalMessageCount = input.TotalNumberOfRecipients;

            await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
        }
    }
}
