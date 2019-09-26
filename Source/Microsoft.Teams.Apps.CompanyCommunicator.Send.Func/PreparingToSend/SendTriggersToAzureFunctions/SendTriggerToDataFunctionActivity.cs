// <copyright file="SendTriggerToDataFunctionActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
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
        private readonly NotificationDataRepositoryFactory notificationDataRepositoryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggerToDataFunctionActivity"/> class.
        /// </summary>
        /// <param name="dataMessageQueue">The message queue service connected to the queue 'company-communicator-data'.</param>
        /// <param name="notificationDataRepositoryFactory">Notification data repository factory.</param>
        public SendTriggerToDataFunctionActivity(
            DataQueue dataMessageQueue,
            NotificationDataRepositoryFactory notificationDataRepositoryFactory)
        {
            this.dataMessageQueue = dataMessageQueue;
            this.notificationDataRepositoryFactory = notificationDataRepositoryFactory;
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
            try
            {
                var queueMessageContent = new DataQueueMessageContent
                {
                    NotificationId = input.NotificationDataEntityId,
                    InitialSendDate = DateTime.UtcNow,
                    TotalMessageCount = input.TotalRecipientCount,
                };
                var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
                serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30);

                await this.dataMessageQueue.SendAsync(serviceBusMessage);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                await this.notificationDataRepositoryFactory.CreateRepository(true)
                    .SaveExceptionInNotificationDataEntityAsync(input.NotificationDataEntityId, ex.Message);
            }
        }
    }
}