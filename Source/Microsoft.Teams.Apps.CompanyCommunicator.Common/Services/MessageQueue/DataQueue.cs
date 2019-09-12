// <copyright file="DataQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The message queue service connected to the "company-communicator-data" queue in Azure service bus.
    /// </summary>
    public class DataQueue : BaseQueue
    {
        private static readonly string DataQueueName = "company-communicator-data";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataQueue"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        public DataQueue(IConfiguration configuration)
            : base(configuration, DataQueue.DataQueueName)
        {
        }
    }
}
