// <copyright file="HandleFailureActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// This class contains the "clean up" durable activity.
    /// If exceptions happen in the "prepare to send" operation, this method is called to log the exception.
    /// </summary>
    public class HandleFailureActivity
    {
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleFailureActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory.</param>
        public HandleFailureActivity(NotificationDataRepositoryFactory notificationDataRepositoryFactory)
        {
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="ex">Exception.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            Exception ex)
        {
            await context.CallActivityWithRetryAsync(
                nameof(HandleFailureActivity.HandleFailureAsync),
                new RetryOptions(TimeSpan.FromSeconds(5), 3),
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
            log.LogError(input.Exception.Message);

            await this.notificationDataRepositoryFactory.CreateRepository(true)
                .SaveExceptionInNotificationDataEntityAsync(input.NotificationDataEntity.Id, input.Exception.Message);
        }
    }
}
