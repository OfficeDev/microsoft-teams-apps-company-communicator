// <copyright file="SendTriggerToDataFunctionActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// Send trigger to the Azure data function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class SendTriggerToDataFunctionActivity
    {
        private readonly DataQueue dataMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggerToDataFunctionActivity"/> class.
        /// </summary>
        /// <param name="dataMessageQueue">The message queue service connected to the queue 'company-communicator-data'.</param>
        public SendTriggerToDataFunctionActivity(DataQueue dataMessageQueue)
        {
            this.dataMessageQueue = dataMessageQueue;
        }

        /// <summary>
        /// Send trigger to the Azure data function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggerToDataFunctionAsync))]
        public async Task SendTriggerToDataFunctionAsync(
            [ActivityTrigger] SendTriggerToDataFunctionActivityDTO input)
        {
            var queueMessageContent = new DataQueueMessageContent
            {
                NotificationId = input.NotificationId,
                InitialSendDate = DateTime.UtcNow,
                TotalMessageCount = input.TotalMessageCount,
            };
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            await this.dataMessageQueue.SendAsync(serviceBusMessage);
        }
    }
}