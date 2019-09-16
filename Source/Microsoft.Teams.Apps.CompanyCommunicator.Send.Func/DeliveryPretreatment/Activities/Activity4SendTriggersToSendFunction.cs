// <copyright file="Activity4SendTriggersToSendFunction.cs" company="Microsoft">
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
    /// Send triggers to the Azure send function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class Activity4SendTriggersToSendFunction
    {
        private readonly SendQueue sendMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity4SendTriggersToSendFunction"/> class.
        /// </summary>
        /// <param name="sendMessageQueue">Send message queue service.</param>
        public Activity4SendTriggersToSendFunction(SendQueue sendMessageQueue)
        {
            this.sendMessageQueue = sendMessageQueue;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="receiverBatches">Receiver batches.</param>
        /// <param name="newSentNotificationId">New sent notification id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            List<List<UserDataEntity>> receiverBatches,
            string newSentNotificationId)
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            var tasks = new List<Task>();

            foreach (var batch in receiverBatches)
            {
                var task = context.CallActivityWithRetryAsync(
                    nameof(Activity4SendTriggersToSendFunction.SendTriggersToSendFunctionAsync),
                    retryOptions,
                    new Activity4SendTriggersToSendFunctionDTO
                    {
                        NewSentNotificationId = newSentNotificationId,
                        ReceiverBatch = batch,
                    });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            context.SetCustomStatus(nameof(Activity4SendTriggersToSendFunction.SendTriggersToSendFunctionAsync));
        }

        /// <summary>
        /// Send trigger to the Azure send function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggersToSendFunctionAsync))]
        public async Task SendTriggersToSendFunctionAsync(
            [ActivityTrigger] Activity4SendTriggersToSendFunctionDTO input)
        {
            var messages = input.ReceiverBatch
                .Select(userDataEntity =>
                {
                    var queueMessageContent = new SendQueueMessageContent
                    {
                        NotificationId = input.NewSentNotificationId,
                        UserDataEntity = userDataEntity,
                    };
                    var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                    return new Message(Encoding.UTF8.GetBytes(messageBody));
                })
                .ToList();

            await this.sendMessageQueue.SendAsync(messages);
        }
    }
}
