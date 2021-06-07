// <copyright file="SendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue
{
    using global::Azure.Messaging.ServiceBus;

    /// <summary>
    /// The message queue service connected to the "company-communicator-send" queue in Azure service bus.
    /// </summary>
    public class SendQueue : BaseQueue<SendQueueMessageContent>, ISendQueue
    {
        /// <summary>
        /// Queue name of the send queue.
        /// </summary>
        public const string QueueName = "company-communicator-send";

        /// <summary>
        /// Initializes a new instance of the <see cref="SendQueue"/> class.
        /// </summary>
        /// <param name="serviceBusClient">The service bus client.</param>
        public SendQueue(ServiceBusClient serviceBusClient)
            : base(
                  serviceBusClient: serviceBusClient,
                  queueName: SendQueue.QueueName)
        {
        }
    }
}
