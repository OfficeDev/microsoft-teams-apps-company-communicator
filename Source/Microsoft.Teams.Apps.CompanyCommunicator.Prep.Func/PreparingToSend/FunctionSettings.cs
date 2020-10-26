// <copyright file="FunctionSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;

    /// <summary>
    /// Function settings.
    /// </summary>
    public class FunctionSettings
    {
        /// <summary>
        /// A default setting for the retry options for starting an activity / sub-orchestrator.
        /// </summary>
        public static readonly RetryOptions DefaultRetryOptions
            = new RetryOptions(
                firstRetryInterval: TimeSpan.FromSeconds(5),
                maxNumberOfAttempts: 3);
    }
}
