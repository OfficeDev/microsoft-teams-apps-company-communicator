// <copyright file="SendDataAggregationMessageActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;

    /// <summary>
    /// This activity sends a message to the data queue to start the aggregation of the results for the given
    /// notification.
    /// </summary>
    public class SendDataAggregationMessageActivity
    {
        private readonly DataQueue dataQueue;
        private readonly double firstDataAggregationMessageDelayInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendDataAggregationMessageActivity"/> class.
        /// </summary>
        /// <param name="dataQueue">The data queue.</param>
        /// <param name="dataQueueMessageOptions">The data queue message options.</param>
        public SendDataAggregationMessageActivity(
            DataQueue dataQueue,
            IOptions<DataQueueMessageOptions> dataQueueMessageOptions)
        {
            this.dataQueue = dataQueue;
            this.firstDataAggregationMessageDelayInSeconds = dataQueueMessageOptions.Value.FirstDataAggregationMessageDelayInSeconds;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationId">The notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            IDurableOrchestrationContext context,
            string notificationId)
        {
            await context.CallActivityWithRetryAsync(
                nameof(SendDataAggregationMessageActivity.SendDataAggregationQueueMessageAsync),
                ActivitySettings.CommonActivityRetryOptions,
                notificationId);
        }

        /// <summary>
        /// Sends a message to the data queue to start the aggregation of the results for the given
        /// notification.
        /// </summary>
        /// <param name="notificationId">The notification Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(SendDataAggregationQueueMessageAsync))]
        public async Task SendDataAggregationQueueMessageAsync(
            [ActivityTrigger] string notificationId)
        {
            var dataQueueMessageContent = new DataQueueMessageContent
            {
                NotificationId = notificationId,
                ForceMessageComplete = false,
            };

            await this.dataQueue.SendDelayedAsync(
                dataQueueMessageContent,
                this.firstDataAggregationMessageDelayInSeconds);
        }
    }
}
