// <copyright file="PrepareToSendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The message queue service connected to the "company-communicator-preapretosend" queue in Azure service bus.
    /// </summary>
    public class PrepareToSendQueue : BaseQueue<PrepareToSendQueueMessageContent>
    {
        /// <summary>
        /// Queue name of the prepare to send queue.
        /// </summary>
        public const string QueueName = "company-communicator-prep";

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareToSendQueue"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        public PrepareToSendQueue(IConfiguration configuration)
            : base(configuration, PrepareToSendQueue.QueueName)
        {
        }
    }
}
