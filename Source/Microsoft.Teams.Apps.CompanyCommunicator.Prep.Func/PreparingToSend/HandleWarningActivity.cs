// <copyright file="HandleWarningActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// This class contains the "clean up" durable activity.
    /// If exceptions happen in the "fetching rosters or group" operation, this method is called to log the warning.
    /// </summary>
    public class HandleWarningActivity
    {
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleWarningActivity"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        public HandleWarningActivity(NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity Id.</param>
        /// <param name="errorMessage">error message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId,
            string errorMessage)
        {
            await context.CallActivityWithRetryAsync(
                nameof(HandleWarningActivity.HandleWarningAsync),
                ActivitySettings.CommonActivityRetryOptions,
                (notificationDataEntityId, errorMessage));
        }

        /// <summary>
        /// This method represents the "clean up" durable activity.
        /// If exceptions happen in the "fetching rosters or group" operation,
        /// this method is called to log the warning.
        /// </summary>
        /// <param name="input">Tuple containing notification id and error message.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(HandleWarningAsync))]
        public async Task HandleWarningAsync(
            [ActivityTrigger](string notificationDataEntityId,
            string errorMessage) input)
        {
            await this.notificationDataRepository
                                .SaveWarningInNotificationDataEntityAsync(input.notificationDataEntityId, input.errorMessage);
        }
    }
}