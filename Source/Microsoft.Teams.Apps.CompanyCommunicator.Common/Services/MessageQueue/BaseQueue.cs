// <copyright file="BaseQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Base message queue service.
    /// It uses Azure service bus queue to store data.
    /// </summary>
    public class BaseQueue
    {
        private static readonly string ServiceBusConnectionConfigurationSettingKey = "ServiceBusConnection";
        private readonly MessageSender messageSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseQueue"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        /// <param name="queueName">Azure service bus queue's name.</param>
        public BaseQueue(IConfiguration configuration, string queueName)
        {
            var serviceBusConnectionString =
                configuration[BaseQueue.ServiceBusConnectionConfigurationSettingKey];
            this.messageSender = new MessageSender(serviceBusConnectionString, queueName);
        }

        /// <summary>
        /// Send a message to Azure service bus queue.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendAsync(Message message)
        {
            await this.messageSender.SendAsync(message);
        }

        /// <summary>
        /// Send a list of messages to Azure service bus queue.
        /// </summary>
        /// <param name="messages">The messages to be sent.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendAsync(List<Message> messages)
        {
            var messageBatches = new List<List<Message>>();

            var totalNumberMessages = messages.Count;
            var batchSize = 100;
            var numberOfCompleteBatches = totalNumberMessages / batchSize;
            var numberMessagesInIncompleteBatch = totalNumberMessages % batchSize;

            for (var i = 0; i < numberOfCompleteBatches; i++)
            {
                var startingIndex = i * batchSize;
                var batch = messages.GetRange(startingIndex, batchSize);
                messageBatches.Add(batch);
            }

            if (numberMessagesInIncompleteBatch != 0)
            {
                var incompleteBatchStartingIndex = numberOfCompleteBatches * batchSize;
                var incompleteBatch = messages.GetRange(
                    incompleteBatchStartingIndex,
                    numberMessagesInIncompleteBatch);
                messageBatches.Add(incompleteBatch);
            }

            // Send batches of messages to the service bus
            foreach (var batch in messageBatches)
            {
                await this.messageSender.SendAsync(batch);
            }
        }
    }
}