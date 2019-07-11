// <copyright file="MustBeValidUpnHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This class is an authorization handler.
    /// It handles the authorization requirement, MustContainUpnClaimRequirement.
    /// </summary>
    public class MustBeValidUpnHandler : AuthorizationHandler<MustBeValidUpnRequirement>
    {
        private readonly bool disableAuthentication;
        private readonly string validUpns;

        /// <summary>
        /// Initializes a new instance of the <see cref="MustBeValidUpnHandler"/> class.
        /// </summary>
        /// <param name="configuration">ASP.NET Core <see cref="IConfiguration"/> instance.</param>
        public MustBeValidUpnHandler(IConfiguration configuration)
        {
            this.disableAuthentication = configuration.GetValue<bool>("DisableAuthentication", true);
            this.validUpns = configuration.GetValue<string>("ValidUpns", string.Empty);
        }

        /// <summary>
        /// This method handles the authorization requirement, MustContainUpnClaimRequirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext instance.</param>
        /// <param name="requirement">MustContainUpnClaimRequirement instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MustBeValidUpnRequirement requirement)
        {
            if (this.disableAuthentication || this.IsValidUpn(context))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Check whether a upn is valid or not.
        /// This is where we should check against the valid list of UPNs
        /// </summary>
        /// <param name="context">Authorization handler context instance.</param>
        /// <returns>Indicate if a upn is valid or not.</returns>
        private bool IsValidUpn(AuthorizationHandlerContext context)
        {
            var claim = context.User?.Claims?.FirstOrDefault(p => p.Type == ClaimTypes.Upn);
            var upn = claim?.Value;
            if (string.IsNullOrWhiteSpace(upn))
            {
                return false;
            }

            var validUpnSet = this.validUpns.Split(',').Select(p => p.Trim()).ToHashSet();
            if (validUpnSet.Count == 0)
            {
                return false;
            }

            return validUpnSet.Contains(upn);
        }
    }
}