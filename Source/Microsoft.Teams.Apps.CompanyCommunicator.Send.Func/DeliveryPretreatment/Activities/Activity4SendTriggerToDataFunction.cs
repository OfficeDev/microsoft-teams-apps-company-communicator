// <copyright file="Activity4SendTriggerToDataFunction.cs" company="Microsoft">
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
    public class Activity4SendTriggerToDataFunction
    {
        private readonly DataQueue dataMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity4SendTriggerToDataFunction"/> class.
        /// </summary>
        /// <param name="dataMessageQueue">The message queue service connected to the queue 'company-communicator-data'.</param>
        public Activity4SendTriggerToDataFunction(DataQueue dataMessageQueue)
        {
            this.dataMessageQueue = dataMessageQueue;
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
            List<List<UserDataEntity>> recipientDataBatches)
        {
            var totalRecipientCount = recipientDataBatches.SelectMany(p => p).Count();

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            await context.CallActivityWithRetryAsync(
                nameof(Activity4SendTriggerToDataFunction.SendTriggerToDataFunctionAsync),
                retryOptions,
                new Activity4SendTriggerToDataFunctionDTO
                {
                    NotificationDataEntityId = notificationDataEntityId,
                    TotalRecipientCount = totalRecipientCount,
                });

            context.SetCustomStatus(nameof(Activity4SendTriggerToDataFunction.SendTriggerToDataFunctionAsync));
        }

        /// <summary>
        /// Send trigger to the Azure data function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggerToDataFunctionAsync))]
        public async Task SendTriggerToDataFunctionAsync(
            [ActivityTrigger] Activity4SendTriggerToDataFunctionDTO input)
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
    }
}