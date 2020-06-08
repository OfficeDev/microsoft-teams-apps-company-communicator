// <copyright file="GraphTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.Apps.CompanyCommunicator.Middleware
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Identity.Web;

    /// <summary>
    /// Add Access Toekn to Graph Api.
    /// </summary>
    public class GraphTokenProvider : IAuthenticationProvider
    {
        private readonly ITokenAcquisition tokenAcquisition;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTokenProvider"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">MSAL.NET token acquisition service.</param>
        public GraphTokenProvider(ITokenAcquisition tokenAcquisition)
        {
            this.tokenAcquisition = tokenAcquisition;
        }

        /// <summary>
        /// Intercepts HttpRequest and add Bearer token.
        /// </summary>
        /// <param name="request">Represents a HttpRequestMessage.</param>
        /// <returns>asynchronous operation.</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // we use MSAL.NET to get a token to call the API On Behalf Of the current user
            var accessToken = await this.tokenAcquisition.GetAccessTokenForUserAsync(new string[] { Common.Constants.ScopeGroupReadAll });

            // Append the access token to the request.
            request.Headers.Authorization = new AuthenticationHeaderValue(
                Common.Constants.BearerAuthorizationScheme, accessToken);
        }
    }
}
