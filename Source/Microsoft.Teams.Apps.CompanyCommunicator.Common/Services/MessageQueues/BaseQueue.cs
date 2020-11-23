// <copyright file="BaseQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
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

        private readonly MessageSender messageSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseQueue{T}"/> class.
        /// </summary>
        /// <param name="serviceBusConnectionString">The service bus connection string.</param>
        /// <param name="queueName">Azure service bus queue's name.</param>
        public BaseQueue(string serviceBusConnectionString, string queueName)
        {
            this.messageSender = new MessageSender(serviceBusConnectionString, queueName);
        }

        /// <inheritdoc/>
        public async Task SendAsync(T queueMessageContent)
        {
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));

            await this.messageSender.SendAsync(serviceBusMessage);
        }

        /// <inheritdoc/>
        public async Task SendAsync(IEnumerable<T> queueMessageContentBatch)
        {
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
                        return new Message(Encoding.UTF8.GetBytes(messageBody));
                    })
                .ToList();

            await this.messageSender.SendAsync(serviceBusMessages);
        }

        /// <inheritdoc/>
        public async Task SendDelayedAsync(T queueMessageContent, double delayNumberOfSeconds)
        {
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody))
            {
                ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(delayNumberOfSeconds),
            };

            await this.messageSender.SendAsync(serviceBusMessage);
        }
    }
}
