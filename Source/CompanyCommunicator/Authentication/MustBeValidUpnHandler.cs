// <copyright file="MustBeValidUpnHandler.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This class is an authorization handler, which handles the authorization requirement.
    /// </summary>
    public class MustBeValidUpnHandler : AuthorizationHandler<MustBeValidUpnRequirement>
    {
        private readonly bool disableCreatorUpnCheck;
        private readonly HashSet<string> authorizedCreatorUpnsSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="MustBeValidUpnHandler"/> class.
        /// </summary>
        /// <param name="authenticationOptions">The authentication options.</param>
        public MustBeValidUpnHandler(IOptions<AuthenticationOptions> authenticationOptions)
        {
            this.disableCreatorUpnCheck = authenticationOptions.Value.DisableCreatorUpnCheck;
            var authorizedCreatorUpns = authenticationOptions.Value.AuthorizedCreatorUpns;
            this.authorizedCreatorUpnsSet = authorizedCreatorUpns
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim())
                ?.ToHashSet()
                ?? new HashSet<string>();
        }

        /// <summary>
        /// This method handles the authorization requirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext instance.</param>
        /// <param name="requirement">IAuthorizationRequirement instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MustBeValidUpnRequirement requirement)
        {
            if (this.disableCreatorUpnCheck || this.IsValidUpn(context))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Check whether a upn (or alternate email for external authors) is valid or not.
        /// This is where we should check against the valid list of UPNs.
        /// </summary>
        /// <param name="context">Authorization handler context instance.</param>
        /// <returns>Indicate if a upn is valid or not.</returns>
        private bool IsValidUpn(AuthorizationHandlerContext context)
        {
            var claimupn = context.User?.Claims?.FirstOrDefault(p => p.Type == ClaimTypes.Upn);
            var upn = claimupn?.Value;

            var claimemail = context.User?.Claims?.FirstOrDefault(p => p.Type == ClaimTypes.Email);
            var email = claimemail?.Value;

            if (string.IsNullOrWhiteSpace(upn) && string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            bool upncheck = this.authorizedCreatorUpnsSet.Contains(upn, StringComparer.OrdinalIgnoreCase);
            bool emailcheck = this.authorizedCreatorUpnsSet.Contains(email, StringComparer.OrdinalIgnoreCase);

            if (upncheck || emailcheck)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
