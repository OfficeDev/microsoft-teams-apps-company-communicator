// <copyright file="SendTriggerToDataFunctionActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// This class contains the "send triggers to Azure data function" durable activity.
    /// </summary>
    public class SendTriggerToDataFunctionActivity
    {
        private readonly DataQueue dataMessageQueue;
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggerToDataFunctionActivity"/> class.
        /// </summary>
        /// <param name="dataMessageQueue">The message queue service connected to the queue 'company-communicator-data'.</param>
        /// <param name="notificationDataRepository">Notification data repository.</param>
        public SendTriggerToDataFunctionActivity(
            DataQueue dataMessageQueue,
            NotificationDataRepository notificationDataRepository)
        {
            this.dataMessageQueue = dataMessageQueue;
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">New sent notification id.</param>
        /// <param name="recipientDataBatches">Recipient data batches.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<IEnumerable<UserDataEntity>> recipientDataBatches)
        {
            var totalRecipientCount = recipientDataBatches.SelectMany(p => p).Count();

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            await context.CallActivityWithRetryAsync(
                nameof(SendTriggerToDataFunctionActivity.SendTriggerToDataFunctionAsync),
                retryOptions,
                new SendTriggerToDataFunctionActivityDTO
                {
                    NotificationDataEntityId = notificationDataEntityId,
                    TotalRecipientCount = totalRecipientCount,
                });
        }

        /// <summary>
        /// This method represents the "send trigger to data function" durable activity.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggerToDataFunctionAsync))]
        public async Task SendTriggerToDataFunctionAsync(
            [ActivityTrigger] SendTriggerToDataFunctionActivityDTO input,
            ILogger log)
        {
            var queueMessageContent = new DataQueueMessageContent
            {
                NotificationId = input.NotificationDataEntityId,
                InitialSendDate = DateTime.UtcNow,
                TotalMessageCount = input.TotalRecipientCount,
            };

            await this.dataMessageQueue.SendAsync(queueMessageContent);

            await this.MarkPreparingToSendIsDone(input.NotificationDataEntityId);
        }

        private async Task MarkPreparingToSendIsDone(string notificationDataEntityId)
        {
            var notificationDataEntity = await this.notificationDataRepository.GetAsync(
                PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                notificationDataEntity.IsPreparingToSend = false;

                await this.notificationDataRepository.CreateOrUpdateAsync(notificationDataEntity);
            }
        }
    }
}