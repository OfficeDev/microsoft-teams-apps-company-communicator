// <copyright file="TeamDataController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;

    /// <summary>
    /// Controller for the teams data.
    /// </summary>
    [Route("api/teamsData")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class TeamDataController
    {
        private readonly TeamDataRepository teamsDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataController"/> class.
        /// </summary>
        /// <param name="teamsDataRepository">Teams data repository instance.</param>
        public TeamDataController(TeamDataRepository teamsDataRepository)
        {
            this.teamsDataRepository = teamsDataRepository;
        }

        /// <summary>
        /// Get all teams data.
        /// </summary>
        /// <returns>A list of team data.</returns>
        [HttpGet("team")]
        public async Task<IEnumerable<TeamData>> GetAllChannelTypeData()
        {
            var entities = await this.teamsDataRepository.All();
            var result = new List<TeamData>();
            foreach (var entity in entities)
            {
                var team = new TeamData
                {
                    TeamId = entity.TeamId,
                    Name = entity.Name,
                };
                result.Add(team);
            }

            return result;
        }
    }
}
