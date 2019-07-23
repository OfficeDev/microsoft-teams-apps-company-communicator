// <copyright file="TeamDataController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;

    /// <summary>
    /// Controller for the teams data.
    /// </summary>
    [Route("api/teamData")]
    [Authorize(PolicyNames.MustBeValidUpnPolicy)]
    public class TeamDataController : ControllerBase
    {
        private readonly TeamDataRepository teamDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataController"/> class.
        /// </summary>
        /// <param name="teamDataRepository">Team data repository instance.</param>
        public TeamDataController(TeamDataRepository teamDataRepository)
        {
            this.teamDataRepository = teamDataRepository;
        }

        /// <summary>
        /// Get data for all teams.
        /// </summary>
        /// <returns>A list of team data.</returns>
        [HttpGet]
        public async Task<IEnumerable<TeamData>> GetAllTeamDataAsync()
        {
            var entities = await this.teamDataRepository.GetAllAsync();
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

        /// <summary>
        /// Get data for teams by Ids.
        /// </summary>
        /// <param name="idsInString">Team ids in string format.</param>
        /// <returns>A list of team data matching the incoming Ids.
        /// If the passed in ids are invalid, it returns 404 not found error.</returns>
        [HttpGet("{idsInString}")]
        public async Task<ActionResult<IEnumerable<TeamData>>> GetTeamDataByIdsAsync(string idsInString)
        {
            if (string.IsNullOrWhiteSpace(idsInString))
            {
                return this.NotFound();
            }

            var ids = idsInString
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim());

            var result = new List<TeamData>();
            foreach (var id in ids)
            {
                var teamData = new TeamData();
                teamData.TeamId = id;
                var teamDataEntity = await this.teamDataRepository.GetAsync(PartitionKeyNames.Metadata.TeamData, id);
                if (teamDataEntity != null)
                {
                    teamData.Name = teamDataEntity.Name;
                }

                result.Add(teamData);
            }

            return this.Ok(result);
        }
    }
}
