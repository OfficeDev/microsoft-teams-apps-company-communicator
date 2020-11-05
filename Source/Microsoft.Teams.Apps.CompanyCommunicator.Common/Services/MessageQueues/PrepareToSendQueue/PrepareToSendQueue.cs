// <copyright file="PrepareToSendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue
{
    using Microsoft.Extensions.Options;

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
        /// <param name="messageQueueOptions">The message queue options.</param>
        public PrepareToSendQueue(IOptions<MessageQueueOptions> messageQueueOptions)
            : base(
                  serviceBusConnectionString: messageQueueOptions.Value.ServiceBusConnection,
                  queueName: PrepareToSendQueue.QueueName)
        {
        }
    }
}
