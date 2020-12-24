// <copyright file="ExportQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue
{
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The message queue service connected to the "company-communicator-export" queue in Azure service bus.
    /// </summary>
    public class ExportQueue : BaseQueue<ExportQueueMessageContent>, IExportQueue
    {
        /// <summary>
        /// Queue name of the export queue.
        /// </summary>
        public const string QueueName = "company-communicator-export";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportQueue"/> class.
        /// </summary>
        /// <param name="messageQueueOptions">The message queue options.</param>
        public ExportQueue(IOptions<MessageQueueOptions> messageQueueOptions)
            : base(
                  serviceBusConnectionString: messageQueueOptions.Value.ServiceBusConnection,
                  queueName: ExportQueue.QueueName)
        {
        }
    }
}
