// <copyright file="IPrepareToSendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue
{
    /// <summary>
    /// interface for Prepare to send Queue.
    /// </summary>
    public interface IPrepareToSendQueue : IBaseQueue<PrepareToSendQueueMessageContent>
    {
    }
}
