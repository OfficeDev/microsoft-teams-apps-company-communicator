// <copyright file="PretreatQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The message queue service connected to the "company-communicator-data" queue in Azure service bus.
    /// </summary>
    public class PretreatQueue : BaseQueue
    {
        private static readonly string PretreatQueueName = "company-communicator-pretreat";

        /// <summary>
        /// Initializes a new instance of the <see cref="PretreatQueue"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        public PretreatQueue(IConfiguration configuration)
            : base(configuration, PretreatQueue.PretreatQueueName)
        {
        }
    }
}
