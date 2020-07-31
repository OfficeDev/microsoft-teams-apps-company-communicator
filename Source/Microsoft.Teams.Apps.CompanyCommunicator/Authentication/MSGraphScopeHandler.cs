// <copyright file="MSGraphScopeHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Identity.Web;

    /// <summary>
    /// This class is an authorization handler, which handles the authorization requirement.
    /// </summary>
    public class MSGraphScopeHandler : AuthorizationHandler<MSGraphScopeRequirement>
    {
        private readonly ITokenAcquisition tokenAcquisition;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSGraphScopeHandler"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">MSAL.NET token acquisition service.</param>
        public MSGraphScopeHandler(ITokenAcquisition tokenAcquisition)
        {
            this.tokenAcquisition = tokenAcquisition;
        }

        /// <summary>
        /// This method handles the authorization requirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext instance.</param>
        /// <param name="requirement">IAuthorizationRequirement instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MSGraphScopeRequirement requirement)
        {
            var hasScope = await this.HasScopesAsync(requirement.Scopes);
            if (hasScope)
            {
                context.Succeed(requirement);
            }
        }

        /// <summary>
        /// Check whether the access token has input scopes.
        /// This is where we should check if user has valid graph access.
        /// </summary>
        /// <param name="scopes">Microsoft Graph scopes.</param>
        /// <returns>Indicate if access token has scope.</returns>
        private async Task<bool> HasScopesAsync(string[] scopes)
        {
            var accessToken = await this.tokenAcquisition.GetAccessTokenForUserAsync(new[] { Common.Constants.ScopeGroupReadAll });
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;
            var claimValue = securityToken.Claims
                .First(claim => claim.Type.Equals(Common.Constants.ClaimTypeScp.ToString(), StringComparison.CurrentCultureIgnoreCase)).Value;
            var intersectScopes = claimValue.ToLower().Split(' ').Intersect(scopes.Select(scp => scp.ToLower())).ToArray();
            return scopes.Length == intersectScopes.Length;
        }
    }
}
