// <copyright file="PollyPolicy.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Policies
{
    using System;
    using System.Net;
    using Microsoft.Graph;
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// Polly policies.
    /// </summary>
    public class PollyPolicy
    {
        /// <summary>
        /// Get the graph retry policy.
        /// </summary>
        /// <param name="maxAttempts">the number of max attempts.</param>
        /// <returns>A retry policy that can be applied to async delegates.</returns>
        public static AsyncRetryPolicy GetGraphRetryPolicy(int maxAttempts)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: maxAttempts);

            // Only Handling 502 Bad Gateway Exception
            // Other exception such as 429, 503, 504 is handled by default by Graph SDK.
            return Policy
                .Handle<ServiceException>(e =>
                e.StatusCode == HttpStatusCode.BadGateway)
                .WaitAndRetryAsync(delay);
        }
    }
}
