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
        /// <summary>
        /// Check if an integer value identifies succeeded.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 201.</returns>
        public static bool IsSucceeded(this int httpStatusCode)
        {
            return httpStatusCode == (int)HttpStatusCode.Created;
        }

        /// <summary>
        /// Check if an integer value identifies throttled.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 500.</returns>
        public static bool IsThrottled(this int httpStatusCode)
        {
            return httpStatusCode == 429;
        }

        /// <summary>
        /// Check if an integer value identifies unknown status.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 0.</returns>
        public static bool IsUnknown(this int httpStatusCode)
        {
            return httpStatusCode == 0;
        }

        /// <summary>
        /// Check if an integer value identifies unknown status.
        /// </summary>
        /// <param name="httpStatusCode">Http status code value.</param>
        /// <returns>Returns true if the http status code is 0.</returns>
        public static bool IsFailed(this int httpStatusCode)
        {
            return httpStatusCode == (int)HttpStatusCode.InternalServerError;
        }
    }
}
