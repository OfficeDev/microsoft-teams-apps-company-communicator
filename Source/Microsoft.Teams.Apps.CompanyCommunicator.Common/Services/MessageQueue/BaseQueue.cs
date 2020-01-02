// <copyright file="BaseQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    /// Base Azure service bus queue service.
    /// </summary>
    /// <typeparam name="T">Queue message class type.</typeparam>
    public class BaseQueue<T>
    {
        /// <summary>
        /// Constant for the service bus connection configuration key.
        /// </summary>
        public const string ServiceBusConnectionConfigurationKey = "ServiceBusConnection";

        /// <summary>
        /// The maximum number of messages that can be in one batch request to the service bus queue.
        /// </summary>
        public static readonly int MaxNumberOfMessagesInBatchRequest = 100;

        private readonly IConfiguration configuration;
        private readonly MessageSender messageSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseQueue{T}"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        /// <param name="queueName">Azure service bus queue's name.</param>
        public BaseQueue(IConfiguration configuration, string queueName)
        {
            this.configuration = configuration;
            var serviceBusConnectionString =
                configuration[BaseQueue<T>.ServiceBusConnectionConfigurationKey];
            this.messageSender = new MessageSender(serviceBusConnectionString, queueName);
        }

        /// <summary>
        /// Sends a message to the Azure service bus queue.
        /// </summary>
        /// <param name="queueMessageContent">Content of the message to be sent to the service bus queue.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendAsync(T queueMessageContent)
        {
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));

            await this.messageSender.SendAsync(serviceBusMessage);
        }

        /// <summary>
        /// Sends a list of messages to the Azure service bus queue.
        /// </summary>
        /// <param name="queueMessageContentBatch">A batch of message contents to be sent to the service bus queue.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendAsync(IEnumerable<T> queueMessageContentBatch)
        {
            // Check that the number of messages to add to the queue in the batch request is not
            // more than the maximum allowed.
            if (queueMessageContentBatch.Count() > BaseQueue<T>.MaxNumberOfMessagesInBatchRequest)
            {
                throw new InvalidOperationException("Exceeded maximum Azure service bus message batch size.");
            }

            // Create batch list of messages to add to the queue.
            var serviceBusMessages = queueMessageContentBatch
                .Select(queueMessageContent =>
                    {
                        var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                        return new Message(Encoding.UTF8.GetBytes(messageBody));
                    })
                .Where(message => message != null)
                .ToList();

            await this.messageSender.SendAsync(serviceBusMessages);
        }

        /// <summary>
        /// Send a delayed message to the Azure service bus queue.
        /// </summary>
        /// <param name="queueMessageContent">Content of the message to be sent.</param>
        /// <param name="delayNumberOfMinues">Number of minutes to delay the sending of the message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendDelayedAsync(T queueMessageContent, int delayNumberOfMinues)
        {
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromMinutes(delayNumberOfMinues);

            await this.messageSender.SendAsync(serviceBusMessage);
        }
    }
}