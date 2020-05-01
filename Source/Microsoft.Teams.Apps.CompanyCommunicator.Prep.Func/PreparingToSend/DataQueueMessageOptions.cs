// <copyright file="DataQueueMessageOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Options for data queue messages.
    /// </summary>
    public class DataQueueMessageOptions
    {
        /// <summary>
        /// Gets or sets the value for the delay to be applied to the data queue message
        /// used as the first message to trigger an aggregation of the current results
        /// of the notifications that have been sent.
        /// </summary>
        public double FirstDataAggregationMessageDelayInSeconds { get; set; }
    }
}
