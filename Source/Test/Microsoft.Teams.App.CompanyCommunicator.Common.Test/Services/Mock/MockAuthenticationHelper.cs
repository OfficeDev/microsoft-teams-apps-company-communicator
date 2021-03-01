// <copyright file="MockAuthenticationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Services.Mock
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Mocking Authentication Provider.
    /// </summary>
    public class MockAuthenticationHelper : IAuthenticationProvider
    {
        /// <summary>
        /// Mock authenticate request.
        /// </summary>
        /// <param name="request">Represents a HttpRequestMessage.</param>
        /// <returns>asynchronous operation.</returns>
        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            return Task.CompletedTask;
        }
    }
}
