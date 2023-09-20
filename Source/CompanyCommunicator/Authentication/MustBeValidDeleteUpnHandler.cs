// <copyright file="MustBeValidDeleteUpnHandler.cs" company="Microsoft">
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
    /// This class is an authorization handler, which handles the authorization requirement for delete messages section.
    /// </summary>
    public class MustBeValidDeleteUpnHandler : AuthorizationHandler<MustBeValidDeleteUpnRequirement>
    {
        private readonly bool disableDeleteUpnCheck;
        private readonly HashSet<string> authorizedDeletionUpnsSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="MustBeValidDeleteUpnHandler"/> class.
        /// </summary>
        /// <param name="authenticationOptions">The authentication options.</param>
        public MustBeValidDeleteUpnHandler(IOptions<AuthenticationOptions> authenticationOptions)
        {
            this.disableDeleteUpnCheck = authenticationOptions.Value.DisableDeleteUpnCheck;
            var authorizedDeleteUpns = authenticationOptions.Value.AuthorizedDeleteUpns;
            this.authorizedDeletionUpnsSet = authorizedDeleteUpns
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
            MustBeValidDeleteUpnRequirement requirement)
        {
            if (this.disableDeleteUpnCheck || this.IsValidDeleteUpn(context))
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
        private bool IsValidDeleteUpn(AuthorizationHandlerContext context)
        {
            var claimupn = context.User?.Claims?.FirstOrDefault(p => p.Type == ClaimTypes.Upn);
            var upn = claimupn?.Value;

            var claimemail = context.User?.Claims?.FirstOrDefault(p => p.Type == ClaimTypes.Email);
            var email = claimemail?.Value;

            if (string.IsNullOrWhiteSpace(upn) && string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            bool upncheck = this.authorizedDeletionUpnsSet.Contains(upn, StringComparer.OrdinalIgnoreCase);
            bool emailcheck = this.authorizedDeletionUpnsSet.Contains(email, StringComparer.OrdinalIgnoreCase);

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
