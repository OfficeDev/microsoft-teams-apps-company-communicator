// <copyright file="SendFunctionOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    /// <summary>
    /// Options used to configure the Company Communicator Send Function.
    /// </summary>
    public class SendFunctionOptions
    {
        /// <summary>
        /// Gets or sets the max number of request attempts.
        /// </summary>
        public int MaxNumberOfAttempts { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds to delay before
        /// retrying to send the message.
        /// </summary>
        public double SendRetryDelayNumberOfSeconds { get; set; }
    }
}
