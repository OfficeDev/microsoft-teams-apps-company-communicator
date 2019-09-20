// <copyright file="BaseQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Base Azure service bus queue service.
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
        /// <param name="messageBatch">The message batch to be sent to service bus queue.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendAsync(List<Message> messageBatch)
        {
            if (messageBatch.Count > 100)
            {
                throw new InvalidOperationException("Exceeded maximum Azure service bus message batch size.");
            }

            await this.messageSender.SendAsync(messageBatch);
        }
    }
}