// <copyright file="MustContainUpnHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Auth
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// This class is an authorization handler.
    /// It handles the authorization requirement, MustContainUpnClaimRequirement.
    /// </summary>
    public class MustContainUpnHandler : AuthorizationHandler<MustContainUpnClaimRequirement>
    {
        /// <summary>
        /// This method handles the authorization requirement, MustContainUpnClaimRequirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext instance.</param>
        /// <param name="requirement">MustContainUpnClaimRequirement instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MustContainUpnClaimRequirement requirement)
        {
            var claim = context.User.Claims.FirstOrDefault(p => p.Type == ClaimTypes.Upn);
            if (claim != null && this.Validate(claim.Value))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validate a upn value.
        /// </summary>
        /// <param name="upn">Upn value.</param>
        /// <returns>Indicate if a upn is valid or not.</returns>
        private bool Validate(string upn)
        {
            return string.IsNullOrWhiteSpace(upn);
        }
    }
}