// <copyright file="ExportControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    using System;
    using System.Collections.Generic;
    using Moq;
    using Xunit;
    using FluentAssertions;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.ExportQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// ExportController test class.
    /// </summary>
    public class ExportControllerTest
    {
        Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        Mock<IExportDataRepository> exportDataRepository = new Mock<IExportDataRepository>();
        Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        Mock<IExportQueue> exportQueue = new Mock<IExportQueue>();
        Mock<ITeamMembersService> memberService = new Mock<ITeamMembersService>();
        Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        Mock<IAppSettingsService> appSettingsService = new Mock<IAppSettingsService>();
        private readonly string claimTypeUserId = "ClaimTypeUserId";
        private readonly string claimTypeTenantId = "ClaimTypeTenantId";


        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, userDataRepository.Object, exportQueue.Object, memberService.Object, teamDataRepository.Object, appSettingsService.Object);

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
            Action action1 = () => new ExportController(null /*sentNotificationDataRepositor*/, exportDataRepository.Object, userDataRepository.Object, exportQueue.Object, memberService.Object, teamDataRepository.Object, appSettingsService.Object);
            Action action2 = () => new ExportController(sentNotificationDataRepository.Object, null /*exportDataRepository*/, userDataRepository.Object, exportQueue.Object, memberService.Object, teamDataRepository.Object, appSettingsService.Object);
            Action action3 = () => new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, null/*userDataRepository*/, exportQueue.Object, memberService.Object, teamDataRepository.Object, appSettingsService.Object);
            Action action4 = () => new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, userDataRepository.Object, null/*exportQueue*/, memberService.Object, teamDataRepository.Object, appSettingsService.Object);
            Action action5 = () => new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, userDataRepository.Object, exportQueue.Object, null/*memberService*/, teamDataRepository.Object, appSettingsService.Object);
            Action action6 = () => new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, userDataRepository.Object, exportQueue.Object, memberService.Object, null/*teamDataRepository*/, appSettingsService.Object);
            Action action7 = () => new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, userDataRepository.Object, exportQueue.Object, memberService.Object, teamDataRepository.Object, null/*appSettingsService*/);

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
        /// <returns></returns>
        [Fact]
        public async Task Export_NullParameter_ThrowsArgumentNullExceptioin()
        {
            // Arrange
            var controller = GetControllerInstance();

            //Act
            Func<Task> task = async () => await controller.ExportNotificationAsync(null /*exportRequest*/);

            //Assert
            await task.Should().ThrowAsync<ArgumentNullException>("exportRequest is null");
        }

        /// <summary>
        /// Test case to verify Application exception when MemberService returns invalidUser.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Get_MemberServiceReturnsInvalidUser_ThrowsApplicationException()
        {
            // Arrange
            var controller = GetControllerInstance();
            var exportRequest = new ExportRequest();
            var serviceUrl = "serviceUrl";
            var userDataList = new List<UserDataEntity>() { new UserDataEntity() { AadId = string.Empty } };
            userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(UserDataEntity)));
            appSettingsService.Setup(x => x.GetServiceUrlAsync()).ReturnsAsync(serviceUrl);
            memberService.Setup(x => x.GetAuthorsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataList);

            // Act
            Func<Task> task = async () => await controller.ExportNotificationAsync(exportRequest);

            // Assert
            await task.Should().ThrowAsync<ApplicationException>();
        }

        /// <summary>
        /// Test case to verify CreateOrUpdateAsync method should get invoked once.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Export_CreateOrUpdateAsync_ShouldInvokeOnce()
        {
            // Arrange
            var controller = GetControllerInstance();
            var exportRequest = new ExportRequest();
            var serviceUrl = "serviceUrl";
            var userDataList = new List<UserDataEntity>() { new UserDataEntity() { AadId = claimTypeUserId } };
            userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(UserDataEntity)));
            appSettingsService.Setup(x => x.GetServiceUrlAsync()).ReturnsAsync(serviceUrl);
            memberService.Setup(x => x.GetAuthorsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataList);
            userDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await controller.ExportNotificationAsync(exportRequest);

            // Assert
            userDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<UserDataEntity>(x => x.AadId == claimTypeUserId)), Times.Once);
        }

        /// <summary>
        /// Test case to verify status code should be conflict (409) on export of the notification which exist in Azure storage.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExportNotification_NotificationExistsInAzureStorage_ReturnsStatusCodeConflict()
        {
            // Arrange
            var controller = GetControllerInstance();
            var userDataEntity = new UserDataEntity();
            var exportRequest = new ExportRequest();
            var exportDataEntity = new ExportDataEntity();
            var StatusCodeConfict = 409;
            userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);
            sentNotificationDataRepository.Setup(x => x.EnsureSentNotificationDataTableExistsAsync()).Returns(Task.CompletedTask);
            exportDataRepository.Setup(x => x.EnsureExportDataTableExistsAsync()).Returns(Task.CompletedTask);
            exportDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(exportDataEntity);

            // Act
            var result = await controller.ExportNotificationAsync(exportRequest);
            var statusCode = ((StatusCodeResult)result).StatusCode;

            // Assert
            Assert.Equal(statusCode, StatusCodeConfict);
        }

        /// <summary>
        /// Test case for export notificaiton returns status code 200 for valid input.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExportNotification_validInput_ReturnsStatusCodeOk()
        {
            // Arrange
            var controller = GetControllerInstance();
            var userDataEntity = new UserDataEntity();
            var exportRequest = new ExportRequest() { Id = "Id" };
            var statusCodeOk = 200;
            var exportDataEntity = new ExportDataEntity()
            {
                RowKey = exportRequest.Id,
                SentDate = DateTime.UtcNow,
                Status = ExportStatus.New.ToString()
            };

            var userDataList = new List<UserDataEntity>() { new UserDataEntity() { AadId = null } };
            userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);
            sentNotificationDataRepository.Setup(x => x.EnsureSentNotificationDataTableExistsAsync()).Returns(Task.CompletedTask);
            exportDataRepository.Setup(x => x.EnsureExportDataTableExistsAsync()).Returns(Task.CompletedTask);
            exportDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(ExportDataEntity)));
            exportDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<ExportDataEntity>())).Returns(Task.CompletedTask);
            exportQueue.Setup(x => x.SendAsync(It.IsAny<ExportQueueMessageContent>())).Returns(Task.CompletedTask);

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
            var controller = new ExportController(sentNotificationDataRepository.Object, exportDataRepository.Object, userDataRepository.Object, exportQueue.Object, memberService.Object, teamDataRepository.Object, appSettingsService.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(Common.Constants.ClaimTypeTenantId, claimTypeTenantId),
                new Claim(Common.Constants.ClaimTypeUserId, claimTypeUserId),

            }, "mock"));

            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            return controller;
        }
    }
}
