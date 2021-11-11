// <copyright file="BaseQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Azure.Messaging.ServiceBus;
    using Newtonsoft.Json;

    /// <summary>
    /// Base Azure service bus queue service.
    /// </summary>
    /// <typeparam name="T">Queue message class type.</typeparam>
    public abstract class BaseQueue<T> : IBaseQueue<T>
    {
        /// <summary>
        /// Constant for the service bus connection configuration key.
        /// </summary>
        public const string ServiceBusConnectionConfigurationKey = "ServiceBusConnection";

        /// <summary>
        /// The maximum number of messages that can be in one batch request to the service bus queue.
        /// </summary>
        public static readonly int MaxNumberOfMessagesInBatchRequest = 100;
        private readonly ServiceBusSender sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseQueue{T}"/> class.
        /// </summary>
        /// <param name="serviceBusClient">The service bus client.</param>
        /// <param name="queueName">Azure service bus queue's name.</param>
        public BaseQueue(ServiceBusClient serviceBusClient, string queueName)
        {
            if (serviceBusClient == null)
            {
                throw new ArgumentNullException(nameof(serviceBusClient));
            }

            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            this.sender = serviceBusClient.CreateSender(queueName);
        }

        /// <inheritdoc/>
        public async Task SendAsync(T queueMessageContent)
        {
            if (queueMessageContent == null)
            {
                throw new ArgumentNullException(nameof(queueMessageContent));
            }

            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new ServiceBusMessage(messageBody);
            await this.sender.SendMessageAsync(serviceBusMessage);
        }

        /// <inheritdoc/>
        public async Task SendAsync(IEnumerable<T> queueMessageContentBatch)
        {
            if (queueMessageContentBatch == null)
            {
                throw new ArgumentNullException(nameof(queueMessageContentBatch));
            }

            var queueMessageContentBatchAsList = queueMessageContentBatch.ToList();

            // Check that the number of messages to add to the queue in the batch request is not
            // more than the maximum allowed.
            if (queueMessageContentBatchAsList.Count > BaseQueue<T>.MaxNumberOfMessagesInBatchRequest)
            {
                throw new InvalidOperationException("Exceeded maximum Azure service bus message batch size.");
            }

            // Create batch list of messages to add to the queue.
            var serviceBusMessages = queueMessageContentBatchAsList
                .Select(queueMessageContent =>
                    {
                        var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                        return new ServiceBusMessage(messageBody);
                    })
                .ToList();

            await this.sender.SendMessagesAsync(serviceBusMessages);
        }

        /// <inheritdoc/>
        public async Task SendDelayedAsync(T queueMessageContent, double delayNumberOfSeconds)
        {
            if (queueMessageContent == null)
            {
                throw new ArgumentNullException(nameof(queueMessageContent));
            }

            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var scheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(delayNumberOfSeconds);
            var serviceBusMessage = new ServiceBusMessage(messageBody);
            await this.sender.ScheduleMessageAsync(serviceBusMessage, scheduledEnqueueTimeUtc);
        }
    }
}
