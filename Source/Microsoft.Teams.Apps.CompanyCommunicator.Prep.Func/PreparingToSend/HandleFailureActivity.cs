// <copyright file="HandleFailureActivity.cs" company="Microsoft">
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
    /// This class contains the "clean up" durable activity.
    /// If exceptions happen in the "prepare to send" operation, this method is called to log the exception.
    /// </summary>
    public class HandleFailureActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleFailureActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        public HandleFailureActivity(NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="ex">Exception.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            Exception ex)
        {
            await context.CallActivityWithRetryAsync(
                nameof(HandleFailureActivity.HandleFailureAsync),
                ActivitySettings.CommonActivityRetryOptions,
                new HandleFailureActivityDTO
                {
                    NotificationDataEntity = notificationDataEntity,
                    Exception = ex,
                });
        }

        /// <summary>
        /// This method represents the "clean up" durable activity.
        /// If exceptions happen in the "prepare to send" operation,
        /// this method is called to do the clean up work, e.g. log the exception and etc.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(HandleFailureAsync))]
        public async Task HandleFailureAsync(
            [ActivityTrigger] HandleFailureActivityDTO input,
            ILogger log)
        {
            var errorMessage = $"Failed to prepare the message for sending: {input.Exception.Message}";

            log.LogError(input.Exception, errorMessage);

            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.SentNotificationsPartition,
                input.NotificationDataEntity.Id);

            if (notificationDataEntity != null)
            {
                notificationDataEntity.IsPreparingToSend = false;
                notificationDataEntity.IsCompleted = true;
                notificationDataEntity.WarningMessage =
                    string.IsNullOrWhiteSpace(notificationDataEntity.WarningMessage)
                    ? errorMessage
                    : $"{notificationDataEntity.WarningMessage}{Environment.NewLine}{errorMessage}";

                // If it failed to prepare for sending a notification, then set the end date to the current date time.
                var currentDate = DateTime.Now;
                notificationDataEntity.SentDate = currentDate;
                notificationDataEntity.SendingStartedDate = currentDate;

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }
    }
}
