// <copyright file="DataQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue
{
    using System;
    using System.Threading.Tasks;
    using global::Azure.Messaging.ServiceBus;

    /// <summary>
    /// The message queue service connected to the "company-communicator-data" queue in Azure service bus.
    /// </summary>
    public class DataQueue : BaseQueue<DataQueueMessageContent>, IDataQueue
    {
        /// <summary>
        /// Queue name of the data queue.
        /// </summary>
        public const string QueueName = "company-communicator-data";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataQueue"/> class.
        /// </summary>
        /// <param name="serviceBusClient">The service bus client.</param>
        public DataQueue(ServiceBusClient serviceBusClient)
            : base(
                  serviceBusClient: serviceBusClient,
                  queueName: DataQueue.QueueName)
        {
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(string notificationId, TimeSpan messageDelay)
        {
            var dataQueueMessageContent = new DataQueueMessageContent
            {
                NotificationId = notificationId,
                ForceMessageComplete = false,
            };

            await this.SendDelayedAsync(
                dataQueueMessageContent,
                messageDelay.TotalSeconds);
        }
    }
}
