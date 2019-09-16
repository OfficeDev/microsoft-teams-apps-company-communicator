// <copyright file="Activity5SendTriggerToDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// Send trigger to the Azure data function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class Activity5SendTriggerToDataFunction
    {
        private readonly DataQueue dataMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity5SendTriggerToDataFunction"/> class.
        /// </summary>
        /// <param name="dataMessageQueue">The message queue service connected to the queue 'company-communicator-data'.</param>
        public Activity5SendTriggerToDataFunction(DataQueue dataMessageQueue)
        {
            this.dataMessageQueue = dataMessageQueue;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="newSentNotificationId">New sent notification id.</param>
        /// <param name="messageBatchesToBeSent">Message batches to be sent to Azure service bus.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            string newSentNotificationId,
            List<List<UserDataEntity>> messageBatchesToBeSent)
        {
            var totalMessagesToBeSentToServiceBusCount = messageBatchesToBeSent.SelectMany(p => p).Count();

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            await context.CallActivityWithRetryAsync(
                nameof(Activity5SendTriggerToDataFunction.SendTriggerToDataFunctionAsync),
                retryOptions,
                new Activity5SendTriggerToDataFunctionDTO
                {
                    NotificationId = newSentNotificationId,
                    TotalMessagesToBeSentToServiceBusCount = totalMessagesToBeSentToServiceBusCount,
                });

            context.SetCustomStatus(nameof(Activity5SendTriggerToDataFunction.SendTriggerToDataFunctionAsync));
        }

        /// <summary>
        /// Send trigger to the Azure data function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggerToDataFunctionAsync))]
        public async Task SendTriggerToDataFunctionAsync(
            [ActivityTrigger] Activity5SendTriggerToDataFunctionDTO input)
        {
            var queueMessageContent = new DataQueueMessageContent
            {
                NotificationId = input.NotificationId,
                InitialSendDate = DateTime.UtcNow,
                TotalMessageCount = input.TotalMessagesToBeSentToServiceBusCount,
            };
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            await this.dataMessageQueue.SendAsync(serviceBusMessage);
        }
    }
}