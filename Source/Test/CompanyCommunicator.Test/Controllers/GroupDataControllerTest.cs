// <copyright file="GroupDataControllerTest.cs" company="Microsoft">
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
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    /// <summary>
    /// GroupDataControllerTest test class.
    /// </summary>
    public class GroupDataControllerTest
    {
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<IGroupsService> groupsService = new Mock<IGroupsService>();

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new GroupDataController(this.notificationDataRepository.Object, this.groupsService.Object);

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
            Action action1 = () => new GroupDataController(null /*notificationDataRepository*/, this.groupsService.Object);
            Action action2 = () => new GroupDataController(this.notificationDataRepository.Object, null /*groupsService*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("groupsService is null.");
        }

        /// <summary>
        /// Test case to validates a draft notification if Team contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_EmptyOrNullParameter_ReturnsNull()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var query = string.Empty;

            // Act
            var result = await controller.SearchAsync(query);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test case to check Group data is null for query param lenth greater than 3.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetGroupData_WithQueryLength3_ReturnsNull()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var query = "val"; // lenght of query is 3

            List<Group> groupList = new List<Group>()
            {
                new Group() { Id = "Id", DisplayName = "name", Mail = "mail" },
            };
            var groupItem = groupList.FirstOrDefault();
            this.groupsService.Setup(x => x.SearchAsync(It.IsAny<string>())).ReturnsAsync(groupList);

            // Act
            var groupData = await controller.SearchAsync(query);
            var groupDataItems = groupData.ToList();

            // Assert
            Assert.IsType<List<GroupData>>(groupDataItems);
        }

        /// <summary>
        /// Test case to check Group data null for query param lenth less than 3.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetGroupData_WithQueryLngthLessThan3_ReturnsNull()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var query = "q"; // lenght of query less then 3

            // Act
            var result = await controller.SearchAsync(query);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test case to check Group data for query param lenth greater than 3.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetGroupData_WithQueryLengthGreaterThan3_ReturnsGroupData()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var query = "query"; // lenght of query greater then 3
            List<Group> groupList = new List<Group>()
            {
                new Group() { Id = "Id", DisplayName = "name", Mail = "mail" },
            };
            var groupItem = groupList.FirstOrDefault();
            this.groupsService.Setup(x => x.SearchAsync(It.IsAny<string>())).ReturnsAsync(groupList);

            // Act
            var groupData = await controller.SearchAsync(query);
            var groupDataItems = groupData.ToList();

            // Assert
            Assert.IsType<List<GroupData>>(groupDataItems);
        }

        /// <summary>
        /// Test case to check if GroupSerarchService method is called once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CallGroupSerarchService_ShouldInvokeOnce()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var query = "query";
            List<Group> group = new List<Group>()
            {
                new Group() { Id = "Id", DisplayName = "name", Mail = "mail" },
            };
            this.groupsService.Setup(x => x.SearchAsync(It.IsAny<string>())).ReturnsAsync(group);

            // Act
            var result = await controller.SearchAsync(query);

            // Assert
            this.groupsService.Verify(x => x.SearchAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case to check if object mapping is correct.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CorrectMapping_ReturnsGroupDataList()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var query = "query";
            List<Group> groupList = new List<Group>()
            {
                new Group() { Id = "Id", DisplayName = "name", Mail = "mail" },
            };
            var groupItem = groupList.FirstOrDefault();
            this.groupsService.Setup(x => x.SearchAsync(It.IsAny<string>())).ReturnsAsync(groupList);

            // Act
            var groupData = await controller.SearchAsync(query);
            var groupDataItem = groupData.FirstOrDefault();

            // Assert
            Assert.Equal(groupDataItem.Id, groupItem.Id);
            Assert.Equal(groupDataItem.Mail, groupItem.Mail);
            Assert.Equal(groupDataItem.Name, groupItem.DisplayName);
        }

        /// <summary>
        /// Test case to validates a draft notification if Team contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_GroupByInvalidId_ReturnsNull()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var id = "GroupId";
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));

            // Act
            var result = await controller.GetGroupsAsync(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        /// <summary>
        /// Test case to Get a draft notification for empty or null Id returns not found result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetGroup_EmptyOrNullParamter_ReturnsNotFoundResult()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));

            // Act
            var result = await controller.GetGroupsAsync(null);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        /// <summary>
        /// Test case to check if GroupSerarchService method is called once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_GroupSearchServiceById_ReturnsOkObjectResult()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var id = "GroupId";
            var group = new List<Group>()
            {
                new Group() { Id = "Id", DisplayName = "name", Mail = "mail" },
            };
            var notificationDataEntity = new NotificationDataEntity() { Groups = new List<string>() };
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);
            this.groupsService.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).Returns(group.ToAsyncEnumerable());

            // Act
            var result = await controller.GetGroupsAsync(id);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Test case to check correct mapping returns Actionresult with Group data list.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_correctMapping_ReturnsGroupDataListObject()
        {
            // Arrange
            var controller = this.GetGroupDataControllerInstance();
            var id = "GroupId";
            var groupList = new List<Group>()
            {
                new Group() { Id = "Id", DisplayName = "name", Mail = "mail" },
            };
            var group = groupList.FirstOrDefault();
            var groupsList = new List<string>() { "G1", "G2" };
            var groupsInStrings = JsonConvert.SerializeObject(groupsList);
            var notificationDataEntity = new NotificationDataEntity() { GroupsInString = groupsInStrings };
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);
            this.groupsService.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).Returns(groupList.ToAsyncEnumerable());

            // Act
            var result = await controller.GetGroupsAsync(id);
            var resultGroupData = (IEnumerable<GroupData>)((ObjectResult)result.Result).Value;
            var groupData = resultGroupData.ToList().FirstOrDefault();

            // Assert
            Assert.Equal(group.Id, groupData.Id);
            Assert.Equal(group.DisplayName, groupData.Name);
            Assert.Equal(group.Mail, groupData.Mail);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupDataController"/> class.
        /// </summary>
        private GroupDataController GetGroupDataControllerInstance()
        {
            return new GroupDataController(this.notificationDataRepository.Object, this.groupsService.Object);
        }
    }
}
