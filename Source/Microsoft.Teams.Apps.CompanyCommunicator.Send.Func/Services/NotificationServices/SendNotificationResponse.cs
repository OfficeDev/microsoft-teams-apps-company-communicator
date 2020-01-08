// <copyright file="SendNotificationResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    using System.Net;

    /// <summary>
    /// The class for the send notification response.
    /// </summary>
    public class SendNotificationResponse
    {
        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the result type.
        /// </summary>
        public SendNotificationResultType ResultType { get; set; }

        /// <summary>
        /// Gets or sets the number of throttle responses.
        /// </summary>
        public int NumberOfThrottleResponses { get; set; }
    }
}
