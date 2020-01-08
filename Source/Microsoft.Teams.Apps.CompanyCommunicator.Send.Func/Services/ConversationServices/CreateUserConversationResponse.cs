// <copyright file="CreateUserConversationResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices
{
    using System.Net;

    /// <summary>
    /// The class for the create user conversation response.
    /// </summary>
    public class CreateUserConversationResponse
    {
        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the result type.
        /// </summary>
        public CreateUserConversationResultType ResultType { get; set; }

        /// <summary>
        /// Gets or sets the conversation ID.
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the number of throttle responses.
        /// </summary>
        public int NumberOfThrottleResponses { get; set; }
    }
}
