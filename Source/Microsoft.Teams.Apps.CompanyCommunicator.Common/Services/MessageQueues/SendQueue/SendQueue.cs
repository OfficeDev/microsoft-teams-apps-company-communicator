// <copyright file="SendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue
{
    using Microsoft.Extensions.Options;

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
        /// <param name="messageQueueOptions">The message queue options.</param>
        public SendQueue(IOptions<MessageQueueOptions> messageQueueOptions)
            : base(
                  serviceBusConnectionString: messageQueueOptions.Value.ServiceBusConnection,
                  queueName: SendQueue.QueueName)
        {
        }
    }
}
