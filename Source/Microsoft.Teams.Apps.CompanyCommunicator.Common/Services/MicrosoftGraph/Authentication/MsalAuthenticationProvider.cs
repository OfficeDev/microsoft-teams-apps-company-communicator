// <copyright file="MsalAuthenticationProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;

    /// <summary>
    /// MSAL Authentication provider for graph calls.
    /// </summary>
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        private readonly IConfidentialClientApplication clientApplication;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="clientApplication">MSAL.NET token acquisition service for confidential clients.</param>
        public MsalAuthenticationProvider(IConfidentialClientApplication clientApplication)
        {
            this.clientApplication = clientApplication ?? throw new ArgumentNullException(nameof(clientApplication));
        }

        /// <inheritdoc/>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var accessToken = await this.GetAccesTokenAsync();

            // Append the access token to the request.
            request.Headers.Authorization = new AuthenticationHeaderValue(
                Common.Constants.BearerAuthorizationScheme, accessToken);
        }

        /// <summary>
        /// gets the access token from confidential client service.
        /// </summary>
        /// <returns>The access token.</returns>
        private async Task<string> GetAccesTokenAsync()
        {
            var scopes = new List<string> { Common.Constants.ScopeDefault, };
            var result = await this.clientApplication.AcquireTokenForClient(scopes)
                .ExecuteAsync();

            return result.AccessToken;
        }
    }
}
