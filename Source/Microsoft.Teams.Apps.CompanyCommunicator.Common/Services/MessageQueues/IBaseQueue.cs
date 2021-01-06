// <copyright file="IBaseQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// interface for Base Queue.
    /// </summary>
    /// <typeparam name="T">Queue message class type.</typeparam>
    public interface IBaseQueue<T>
    {
        /// <summary>
        /// Sends a message to the Azure service bus queue.
        /// </summary>
        /// <param name="queueMessageContent">Content of the message to be sent to the service bus queue.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task SendAsync(T queueMessageContent);

        /// <summary>
        /// Sends a list of messages to the Azure service bus queue.
        /// </summary>
        /// <param name="queueMessageContentBatch">A batch of message contents to be sent to the service bus queue.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task SendAsync(IEnumerable<T> queueMessageContentBatch);

        /// <summary>
        /// Send message marked with a delay to the Azure service bus queue.
        /// </summary>
        /// <param name="queueMessageContent">Content of the message to be sent.</param>
        /// <param name="delayNumberOfSeconds">Number of seconds to apply as a delay to the message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task SendDelayedAsync(T queueMessageContent, double delayNumberOfSeconds);
    }
}
