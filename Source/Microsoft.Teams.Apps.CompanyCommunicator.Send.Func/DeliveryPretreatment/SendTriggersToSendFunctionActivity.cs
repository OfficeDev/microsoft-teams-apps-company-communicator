// <copyright file="SendTriggersToSendFunctionActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// Send triggers to the Azure send function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class SendTriggersToSendFunctionActivity
    {
        private readonly SendQueue sendMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggersToSendFunctionActivity"/> class.
        /// </summary>
        public SendTriggersToSendFunctionActivity(SendQueue sendMessageQueue)
        {
            this.sendMessageQueue = sendMessageQueue;
        }

        /// <summary>
        /// Send trigger to the Azure send function.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggersToSendFunctionAsync))]
        public async Task SendTriggersToSendFunctionAsync(
            [ActivityTrigger] SendTriggersToSendFunctionActivityDTO input
)
        {
            var allServiceBusMessages = input.DeduplicatedReceiverEntities
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

            await this.sendMessageQueue.SendAsync(allServiceBusMessages);
        }
    }
}
