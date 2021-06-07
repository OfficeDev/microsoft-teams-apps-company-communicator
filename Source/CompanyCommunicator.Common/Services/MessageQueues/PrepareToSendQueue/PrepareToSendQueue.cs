// <copyright file="PrepareToSendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue
{
    using global::Azure.Messaging.ServiceBus;

    /// <summary>
    /// The message queue service connected to the "company-communicator-prep" queue in Azure service bus.
    /// </summary>
    public class PrepareToSendQueue : BaseQueue<PrepareToSendQueueMessageContent>, IPrepareToSendQueue
    {
        /// <summary>
        /// Queue name of the prepare to send queue.
        /// </summary>
        public const string QueueName = "company-communicator-prep";

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareToSendQueue"/> class.
        /// </summary>
        /// <param name="serviceBusClient">The service bus client.</param>
        public PrepareToSendQueue(ServiceBusClient serviceBusClient)
            : base(
                  serviceBusClient: serviceBusClient,
                  queueName: PrepareToSendQueue.QueueName)
        {
        }
    }
}
