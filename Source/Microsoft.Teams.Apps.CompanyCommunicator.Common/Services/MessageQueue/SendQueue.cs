// <copyright file="SendQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The message queue service connected to the "company-communicator-send" queue in Azure service bus.
    /// </summary>
    public class SendQueue : BaseQueue<SendQueueMessageContent>
    {
        private static readonly string SendQueueName = "company-communicator-send";

        /// <summary>
        /// Initializes a new instance of the <see cref="SendQueue"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        public SendQueue(IConfiguration configuration)
            : base(configuration, SendQueue.SendQueueName)
        {
        }
    }
}
