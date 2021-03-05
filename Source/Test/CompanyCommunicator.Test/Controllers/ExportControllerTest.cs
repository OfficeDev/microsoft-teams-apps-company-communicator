// <copyright file="ExportControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Moq;
    using Xunit;

    /// <summary>
    /// ExportController test class.
    /// </summary>
    public class ExportControllerTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<IExportDataRepository> exportDataRepository = new Mock<IExportDataRepository>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<IExportQueue> exportQueue = new Mock<IExportQueue>();
        private readonly Mock<ITeamMembersService> memberService = new Mock<ITeamMembersService>();
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        private readonly Mock<IAppSettingsService> appSettingsService = new Mock<IAppSettingsService>();
        private readonly string claimTypeUserId = "ClaimTypeUserId";
        private readonly string claimTypeTenantId = "ClaimTypeTenantId";

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, this.userDataRepository.Object, this.exportQueue.Object, this.memberService.Object, this.teamDataRepository.Object, this.appSettingsService.Object);

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
            Action action1 = () => new ExportController(null /*sentNotificationDataRepositor*/, this.exportDataRepository.Object, this.userDataRepository.Object, this.exportQueue.Object, this.memberService.Object, this.teamDataRepository.Object, this.appSettingsService.Object);
            Action action2 = () => new ExportController(this.sentNotificationDataRepository.Object, null /*exportDataRepository*/, this.userDataRepository.Object, this.exportQueue.Object, this.memberService.Object, this.teamDataRepository.Object, this.appSettingsService.Object);
            Action action3 = () => new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, null/*userDataRepository*/, this.exportQueue.Object, this.memberService.Object, this.teamDataRepository.Object, this.appSettingsService.Object);
            Action action4 = () => new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, this.userDataRepository.Object, null/*exportQueue*/, this.memberService.Object, this.teamDataRepository.Object, this.appSettingsService.Object);
            Action action5 = () => new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, this.userDataRepository.Object, this.exportQueue.Object, null/*memberService*/, this.teamDataRepository.Object, this.appSettingsService.Object);
            Action action6 = () => new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, this.userDataRepository.Object, this.exportQueue.Object, this.memberService.Object, null/*teamDataRepository*/, this.appSettingsService.Object);
            Action action7 = () => new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, this.userDataRepository.Object, this.exportQueue.Object, this.memberService.Object, this.teamDataRepository.Object, null/*appSettingsService*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("draftNotificationPreviewService is null.");
            action4.Should().Throw<ArgumentNullException>("appSettingsService is null.");
            action5.Should().Throw<ArgumentNullException>("localizer is null.");
            action6.Should().Throw<ArgumentNullException>("groupsService is null.");
            action7.Should().Throw<ArgumentNullException>("appSettingsService is null.");
        }

        /// <summary>
        /// Test case for null parameter input throws ArgumentNullException.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Export_NullParameter_ThrowsArgumentNullExceptioin()
        {
            // Arrange
            var controller = this.GetControllerInstance();

            // Act
            Func<Task> task = async () => await controller.ExportNotificationAsync(null /*exportRequest*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("exportRequest is null");
        }

        /// <summary>
        /// Test case to verify Application exception when MemberService returns invalidUser.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_MemberServiceReturnsInvalidUser_ThrowsApplicationException()
        {
            // Arrange
            var controller = this.GetControllerInstance();
            var exportRequest = new ExportRequest();
            var serviceUrl = "serviceUrl";
            var userDataList = new List<UserDataEntity>() { new UserDataEntity() { AadId = string.Empty } };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(UserDataEntity)));
            this.appSettingsService.Setup(x => x.GetServiceUrlAsync()).ReturnsAsync(serviceUrl);
            this.memberService.Setup(x => x.GetAuthorsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataList);

            // Act
            Func<Task> task = async () => await controller.ExportNotificationAsync(exportRequest);

            // Assert
            await task.Should().ThrowAsync<ApplicationException>();
        }

        /// <summary>
        /// Test case to verify CreateOrUpdateAsync method should get invoked once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Export_CreateOrUpdateAsync_ShouldInvokeOnce()
        {
            // Arrange
            var controller = this.GetControllerInstance();
            var exportRequest = new ExportRequest();
            var serviceUrl = "serviceUrl";
            var userDataList = new List<UserDataEntity>() { new UserDataEntity() { AadId = this.claimTypeUserId } };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(UserDataEntity)));
            this.appSettingsService.Setup(x => x.GetServiceUrlAsync()).ReturnsAsync(serviceUrl);
            this.memberService.Setup(x => x.GetAuthorsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataList);
            this.userDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await controller.ExportNotificationAsync(exportRequest);

            // Assert
            this.userDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<UserDataEntity>(x => x.AadId == this.claimTypeUserId)), Times.Once);
        }

        /// <summary>
        /// Test case to verify status code should be conflict (409) on export of the notification which exist in Azure storage.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ExportNotification_NotificationExistsInAzureStorage_ReturnsStatusCodeConflict()
        {
            // Arrange
            var controller = this.GetControllerInstance();
            var userDataEntity = new UserDataEntity();
            var exportRequest = new ExportRequest();
            var exportDataEntity = new ExportDataEntity();
            var statusCodeConfict = 409;
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);
            this.sentNotificationDataRepository.Setup(x => x.EnsureSentNotificationDataTableExistsAsync()).Returns(Task.CompletedTask);
            this.exportDataRepository.Setup(x => x.EnsureExportDataTableExistsAsync()).Returns(Task.CompletedTask);
            this.exportDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(exportDataEntity);

            // Act
            var result = await controller.ExportNotificationAsync(exportRequest);
            var statusCode = ((StatusCodeResult)result).StatusCode;

            // Assert
            Assert.Equal(statusCode, statusCodeConfict);
        }

        /// <summary>
        /// Test case for export notificaiton returns status code 200 for valid input.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ExportNotification_validInput_ReturnsStatusCodeOk()
        {
            // Arrange
            var controller = this.GetControllerInstance();
            var userDataEntity = new UserDataEntity();
            var exportRequest = new ExportRequest() { Id = "Id" };
            var statusCodeOk = 200;
            var exportDataEntity = new ExportDataEntity()
            {
                RowKey = exportRequest.Id,
                SentDate = DateTime.UtcNow,
                Status = ExportStatus.New.ToString(),
            };

            var userDataList = new List<UserDataEntity>() { new UserDataEntity() { AadId = null } };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);
            this.sentNotificationDataRepository.Setup(x => x.EnsureSentNotificationDataTableExistsAsync()).Returns(Task.CompletedTask);
            this.exportDataRepository.Setup(x => x.EnsureExportDataTableExistsAsync()).Returns(Task.CompletedTask);
            this.exportDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(ExportDataEntity)));
            this.exportDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<ExportDataEntity>())).Returns(Task.CompletedTask);
            this.exportQueue.Setup(x => x.SendAsync(It.IsAny<ExportQueueMessageContent>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.ExportNotificationAsync(exportRequest);
            var statusCode = ((StatusCodeResult)result).StatusCode;

            // Assert
            Assert.Equal(statusCode, statusCodeOk);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationsController"/> class.
        /// </summary>
        private ExportController GetControllerInstance()
        {
            var controller = new ExportController(this.sentNotificationDataRepository.Object, this.exportDataRepository.Object, this.userDataRepository.Object, this.exportQueue.Object, this.memberService.Object, this.teamDataRepository.Object, this.appSettingsService.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new Claim[]
            {
                new Claim(Common.Constants.ClaimTypeTenantId, this.claimTypeTenantId),
                new Claim(Common.Constants.ClaimTypeUserId, this.claimTypeUserId),
            }, "mock"));

            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            return controller;
        }
    }
}
