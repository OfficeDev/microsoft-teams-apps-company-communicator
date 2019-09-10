// <copyright file="HttpStatusCodeExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Helpers
{
    using System.Net;

    /// <summary>
    /// Http status code extension methods class.
    /// </summary>
    public static class HttpStatusCodeExtensions
    {
        private static readonly int Unknown = 0;
        private static readonly int TooManyRequest = 429;

        /// <summary>
        /// Check if an integer value identifies created status.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 201.</returns>
        public static bool IsSucceeded(this int httpStatusCode)
        {
            return httpStatusCode == (int)HttpStatusCode.Created;
        }

        /// <summary>
        /// Check if an integer value identifies too many request status.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 429.</returns>
        public static bool IsThrottled(this int httpStatusCode)
        {
            return httpStatusCode == HttpStatusCodeExtensions.TooManyRequest;
        }

        /// <summary>
        /// Check if an integer value identifies unknown status.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 0.</returns>
        public static bool IsUnknown(this int httpStatusCode)
        {
            return httpStatusCode == HttpStatusCodeExtensions.Unknown;
        }

        /// <summary>
        /// Check if an integer value identifies internal server error status.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 500.</returns>
        public static bool IsFailed(this int httpStatusCode)
        {
            return httpStatusCode != (int)HttpStatusCode.Created
                && httpStatusCode != HttpStatusCodeExtensions.TooManyRequest
                && httpStatusCode != HttpStatusCodeExtensions.Unknown;
        }
    }
}
