// <copyright file="UpdateNotificationStatusActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Update notification status activity.
    /// </summary>
    public class UpdateNotificationStatusActivity
    {
        private readonly INotificationDataRepository notificationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateNotificationStatusActivity"/> class.
        /// </summary>
        /// <param name="notificationRepository">Notification data repository.</param>
        public UpdateNotificationStatusActivity(INotificationDataRepository notificationRepository)
        {
            this.notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        }

        /// <summary>
        /// Updates notification status.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.UpdateNotificationStatusActivity)]
        public async Task RunAsync(
            [ActivityTrigger](string notificationId, NotificationStatus status) input)
        {
            if (input.notificationId == null)
            {
                throw new ArgumentNullException(nameof(input.notificationId));
            }

            await this.notificationRepository.UpdateNotificationStatusAsync(input.notificationId, input.status);
        }
    }
}
