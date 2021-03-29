// <copyright file="ISendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue
{
    /// <summary>
    /// interface for Send Queue.
    /// </summary>
    public interface ISendQueue : IBaseQueue<SendQueueMessageContent>
    {
    }
}
