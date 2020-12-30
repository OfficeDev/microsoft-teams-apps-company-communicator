// <copyright file="IDataQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue
{
    /// <summary>
    /// interface for DataQueue.
    /// </summary>
    public interface IDataQueue : IBaseQueue<DataQueueMessageContent>
    {
    }
}
