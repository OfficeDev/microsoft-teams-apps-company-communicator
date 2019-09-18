// <copyright file="CleanUpActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Send triggers to the Azure send function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class CleanUpActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanUpActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public CleanUpActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
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
            await context.CallActivityAsync(
                nameof(CleanUpActivity.CleanUp),
                new CleanUpActivityDTO
                {
                    NotificationDataEntity = notificationDataEntity,
                    Exception = ex,
                });
        }

        /// <summary>
        /// Send trigger to the Azure send function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(CleanUp))]
        public async Task CleanUp([ActivityTrigger] CleanUpActivityDTO input)
        {
            await this.metadataProvider.SaveExceptionInNotificationDataEntityAsync(
                input.NotificationDataEntity.Id,
                input.Exception.Message);
        }
    }
}
