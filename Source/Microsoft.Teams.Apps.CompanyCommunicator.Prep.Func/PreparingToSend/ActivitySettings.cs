// <copyright file="ActivitySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;

    /// <summary>
    /// Settings for Activity classes for durable tasks.
    /// </summary>
    public class ActivitySettings
    {
        /// <summary>
        /// A common setting for the retry options for starting an activity.
        /// </summary>
        public static readonly RetryOptions CommonActivityRetryOptions
            = new RetryOptions(
                firstRetryInterval: TimeSpan.FromSeconds(5),
                maxNumberOfAttempts: 3);
    }
}
