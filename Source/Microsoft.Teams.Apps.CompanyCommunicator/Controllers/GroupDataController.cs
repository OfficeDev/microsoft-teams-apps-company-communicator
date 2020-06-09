// <copyright file="GroupDataController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Identity.Web;
    using Microsoft.Identity.Web.Resource;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Controller for getting groups.
    /// </summary>
    [Route("api/groupData")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class GroupDataController : Controller
    {
        private readonly string[] scopeRequiredByAPI = new string[] { "access_as_user" };
        private readonly ITokenAcquisition tokenAcquisition;
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly IMicrosoftGraphService microsoftGraphService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupDataController"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition from MSAL.</param>
        /// <param name="notificationDataRepository">Notification data repository instance.</param>
        /// <param name="microsoftGraphService">Microsoft Graph service instance.</param>
        public GroupDataController(
            ITokenAcquisition tokenAcquisition,
            NotificationDataRepository notificationDataRepository,
            IMicrosoftGraphService microsoftGraphService)
        {
            this.tokenAcquisition = tokenAcquisition;
            this.notificationDataRepository = notificationDataRepository;
            this.microsoftGraphService = microsoftGraphService;
        }

        /// <summary>
        /// Check if user has group.read.all access.
        /// </summary>
        /// <returns>boolean.</returns>
        [HttpGet("verifyaccess")]
        [AuthorizeForScopes(Scopes = new string[] { Common.Constants.ScopeGroupReadAll })]
        public async Task<bool> VerifyAccess()
        {
            // we use MSAL.NET to get a token to call the API On Behalf Of the current user
            var accessToken = await this.tokenAcquisition.GetAccessTokenForUserAsync(new[] { Common.Constants.ScopeUserRead });
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;
            var claimValue = securityToken.Claims.First(claim => claim.Type.ToLower() == Common.Constants.ClaimTypeScp).Value;
            return claimValue.ToLower().Split(' ').Contains(Common.Constants.ScopeGroupReadAll.ToLower());
        }

        /// <summary>
        /// Action method to get groups.
        /// </summary>
        /// <param name="query">user input.</param>
        /// <returns>list of audience.</returns>
        [HttpGet("search/{query}")]
        [AuthorizeForScopes(Scopes = new[] { Common.Constants.ScopeGroupReadAll })]
        public async Task<IEnumerable<AudienceData>> SearchAsync(string query)
        {
            var groups = await this.microsoftGraphService.SearchGroupsAsync(query);
            return groups.Select(group => new AudienceData()
            {
                Id = group.Id,
                Name = string.IsNullOrEmpty(group.Mail) ? group.DisplayName : group.Mail,
            });
        }

        /// <summary>
        /// Get Group Names by Id.
        /// </summary>
        /// <param name="id">Draft notification Id.</param>
        /// <returns>List of Group Names.</returns>
        [HttpGet("{id}")]
        [AuthorizeForScopes(Scopes = new[] { Common.Constants.ScopeGroupReadAll })]
        public async Task<ActionResult<IEnumerable<AudienceData>>> GetGroupsAsync(string id)
        {
            var notificationEntity = await this.notificationDataRepository.GetAsync(
                NotificationDataTableNames.DraftNotificationsPartition,
                id);
            if (notificationEntity == null)
            {
                return this.NotFound();
            }

            var groups = await this.microsoftGraphService.GetGroupByIdsAsync(notificationEntity.Groups.ToList());
            var audience = groups.Select(group => new AudienceData()
            {
                Id = group.Id,
                Name = string.IsNullOrEmpty(group.Mail) ? group.DisplayName : group.Mail,
            });

            return this.Ok(audience);
        }
    }
}
