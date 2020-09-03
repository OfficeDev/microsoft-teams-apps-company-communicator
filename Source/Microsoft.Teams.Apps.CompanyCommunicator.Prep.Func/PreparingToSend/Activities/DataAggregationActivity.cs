// <copyright file="DataAggregationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;

    /// <summary>
    /// This activity sends a message to the data queue to start aggregating results
    /// for a given notification.
    /// </summary>
    public class DataAggregationActivity
    {
        private readonly DataQueue dataQueue;
        private readonly double messageDelayInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAggregationActivity"/> class.
        /// </summary>
        /// <param name="dataQueue">The data queue.</param>
        /// <param name="options">The data queue message options.</param>
        public DataAggregationActivity(
            DataQueue dataQueue,
            IOptions<DataQueueMessageOptions> options)
        {
            this.dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
            this.messageDelayInSeconds = options?.Value?.MessageDelayInSeconds ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Sends a message to the data queue to start the aggregation of the results for the given
        /// notification.
        /// </summary>
        /// <param name="notificationId">The notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.DataAggregationActivity)]
        public async Task RunAsync(
            [ActivityTrigger] string notificationId)
        {
            var dataQueueMessageContent = new DataQueueMessageContent
            {
                NotificationId = notificationId,
                ForceMessageComplete = false,
            };

            await this.dataQueue.SendDelayedAsync(
                dataQueueMessageContent,
                this.messageDelayInSeconds);
        }
    }
}
