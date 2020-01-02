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
        /// The maximum number of messages that can be in one batch request to the service bus queue.
        /// </summary>
        public static readonly int MaxNumberOfMessagesInBatchRequest = 100;

        private static readonly string ServiceBusConnectionConfigurationSettingKey = "ServiceBusConnection";
        private static readonly string SendDelayedRetryNumberOfMinutesConfigurationSettingKey = "SendDelayedRetryNumberOfMinutes";
        private static readonly int DefaultDelayedRetryNumberOfMinutes = 11;
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
                configuration[BaseQueue<T>.ServiceBusConnectionConfigurationSettingKey];
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
        /// Send a delayed message to the Azure service bus queue. Delay time is configured in
        /// the configuration settings.
        /// </summary>
        /// <param name="queueMessageContent">Content of the message to be sent.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendDelayedRetryAsync(T queueMessageContent)
        {
            // Simply initialize the variable for certain build environments and versions
            var sendDelayedRetryNumberOfMinutes = 0;

            // If parsing fails, out variable is set to 0, so need to set the default
            if (!int.TryParse(
                this.configuration[BaseQueue<T>.SendDelayedRetryNumberOfMinutesConfigurationSettingKey],
                out sendDelayedRetryNumberOfMinutes))
            {
                sendDelayedRetryNumberOfMinutes = BaseQueue<T>.DefaultDelayedRetryNumberOfMinutes;
            }

            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromMinutes(sendDelayedRetryNumberOfMinutes);

            await this.messageSender.SendAsync(serviceBusMessage);
        }
    }
}