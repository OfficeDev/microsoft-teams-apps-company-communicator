// <copyright file="ISendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
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
