// <copyright file="IDataQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// interface for DataQueue.
    /// </summary>
    public interface IDataQueue : IBaseQueue<DataQueueMessageContent>
    {
        /// <summary>
        /// Sends message to data queue to trigger Data function.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <param name="messageDelay">time to delay the message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendMessageAsync(string notificationId, TimeSpan messageDelay);
    }
}
