// <copyright file="TeamDataControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Moq;
    using Xunit;

    /// <summary>
    /// TeamDataController test class.
    /// </summary>
    public class TeamDataControllerTest
    {
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new TeamDataController(this.teamDataRepository.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Constructor test for null parameter.
        /// </summary>
        [Fact]
        public void CreateInstance_NullParameter_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new TeamDataController(null /*teamDataRepository*/);

            // Act and Assert.
            action.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
        }

        /// <summary>
        /// Test case to verity the get team data with correct mapping returns teamData list object.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetTeamData_CorrectMapping_ReturnsTeamDataListObject()
        {
            var controller = this.GetControllerInstance();
            var teamDataEntityList = new List<TeamDataEntity>()
            {
                new TeamDataEntity() { TeamId = "teamId", Name = "teamName" },
            };
            var teamDataEntity = teamDataEntityList.FirstOrDefault();
            this.teamDataRepository.Setup(x => x.GetAllSortedAlphabeticallyByNameAsync()).ReturnsAsync(teamDataEntityList);

            // Act
            var result = await controller.GetAllTeamDataAsync();
            var teamDataList = result.ToList();
            var teamData = teamDataList.FirstOrDefault();

            // Assert
            Assert.Equal(teamData.Id, teamDataEntity.TeamId);
            Assert.Equal(teamData.Name, teamDataEntity.Name);
        }

        /// <summary>
        /// Test case to verify team data response is empty if no items exists in DB.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetTeamData_NoItemsExistsInDB_ReturnsEmptyTeamDataList()
        {
            var controller = this.GetControllerInstance();
            var teamDataEntityList = new List<TeamDataEntity>();
            this.teamDataRepository.Setup(x => x.GetAllSortedAlphabeticallyByNameAsync()).ReturnsAsync(teamDataEntityList);

            // Act
            var result = await controller.GetAllTeamDataAsync();
            var teamDataList = result.ToList();

            // Assert
            Assert.Empty(teamDataList);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDataController"/> class.
        /// </summary>
        private TeamDataController GetControllerInstance()
        {
            return new TeamDataController(this.teamDataRepository.Object);
        }
    }
}
