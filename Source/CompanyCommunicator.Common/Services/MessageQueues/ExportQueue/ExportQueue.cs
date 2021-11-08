// <copyright file="ExportQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue
{
    using global::Azure.Messaging.ServiceBus;

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
        /// <param name="serviceBusClient">The service bus client.</param>
        public ExportQueue(ServiceBusClient serviceBusClient)
            : base(
                  serviceBusClient: serviceBusClient,
                  queueName: ExportQueue.QueueName)
        {
        }
    }
}
