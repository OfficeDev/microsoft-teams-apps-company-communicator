// <copyright file="TeamsDataController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User;

    /// <summary>
    /// Teams data controller.
    /// </summary>
    [Route("api/teamsData")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class TeamsDataController
    {
        private readonly TeamsDataRepository teamsDataRepository;
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsDataController"/> class.
        /// </summary>
        /// <param name="teamsDataRepository">Teams data repository instance.</param>
        /// <param name="userDataRepository">User data repository instance.</param>
        public TeamsDataController(
            TeamsDataRepository teamsDataRepository,
            UserDataRepository userDataRepository)
        {
            this.teamsDataRepository = teamsDataRepository;
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Get all teams data.
        /// </summary>
        /// <returns>A list of team data.</returns>
        [HttpGet("channel")]
        public IEnumerable<Team> GetAllChannelTypeData()
        {
            var entities = this.teamsDataRepository.All();
            var result = new List<Team>();
            foreach (var entity in entities)
            {
                var team = new Team
                {
                    TeamId = entity.TeamId,
                    Name = entity.Name,
                    ServiceUrl = entity.ServiceUrl,
                    TenantId = entity.TenantId,
                };
                result.Add(team);
            }

            return result;
        }

        /// <summary>
        /// Get all users' data.
        /// </summary>
        /// <returns>A list of user data.</returns>
        [HttpGet("personal")]
        public IEnumerable<User> GetAllPersonalTypeData()
        {
            var entities = this.userDataRepository.All();
            var result = new List<User>();
            foreach (var entity in entities)
            {
                var user = new User
                {
                    Name = entity.Name,
                    Email = entity.Email,
                    Upn = entity.Upn,
                    AadId = entity.AadId,
                    UserId = entity.UserId,
                    ConversationId = entity.ConversationId,
                    ServiceUrl = entity.ServiceUrl,
                    TenantId = entity.TenantId,
                };
                result.Add(user);
            }

            return result;
        }
    }
}
