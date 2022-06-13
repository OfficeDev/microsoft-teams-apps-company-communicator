// <copyright file="DraftNotificationsControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

using System.IO;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
using Microsoft.Teams.Apps.CompanyCommunicator.Controllers.Options;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Microsoft.Teams.Apps.CompanyCommunicator.DraftNotificationPreview;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;
    using Moq;
    using Xunit;

    /// <summary>
    /// DraftNotificationsController test class.
    /// </summary>
    public class DraftNotificationsControllerTest
    {
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        private readonly Mock<IDraftNotificationPreviewService> draftNotificationPreviewService = new Mock<IDraftNotificationPreviewService>();
        private readonly Mock<IGroupsService> groupsService = new Mock<IGroupsService>();
        private readonly Mock<IAppSettingsService> appSettingsService = new Mock<IAppSettingsService>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly string notificationId = "notificationId";

        private readonly Mock<IStorageClientFactory> storageClientFactory = new Mock<IStorageClientFactory>();
        private readonly Mock<IOptions<UserAppOptions>> userAppOptions = new Mock<IOptions<UserAppOptions>>();
        private bool imageUploadBlobStorage;

        /// <summary>
        /// Gets DraftPreviewPrams.
        /// </summary>
        public static IEnumerable<object[]> DraftPreviewPrams
        {
            get
            {
                return new[]
                {
                    new object[] { null /*draftNotificationPreviewRequest*/, "draftNotificationId", "teamsTeamId", "teamsChannelId" },
                    new object[] { new DraftNotificationPreviewRequest(), null /*draftNotificationId*/, "teamsTeamId", "teamsChannelId" },
                    new object[] { new DraftNotificationPreviewRequest(), "draftNotificationId", null /*teamsTeamId*/, "teamsChannelId" },
                    new object[] { new DraftNotificationPreviewRequest(), "draftNotificationId", "teamsTeamId", null /*teamsChannelId*/ },
                };
            }
        }

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            this.userAppOptions.Setup(x => x.Value).Returns(new UserAppOptions());

            // Arrange
            Action action = () => new DraftNotificationsController(this.notificationDataRepository.Object, this.teamDataRepository.Object, this.draftNotificationPreviewService.Object, this.appSettingsService.Object, this.localizer.Object, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);

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
            Action action1 = () => new DraftNotificationsController(null /*notificationDataRepository*/, this.teamDataRepository.Object, this.draftNotificationPreviewService.Object, this.appSettingsService.Object, this.localizer.Object, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);
            Action action2 = () => new DraftNotificationsController(this.notificationDataRepository.Object, null /*teamDataRepository*/, this.draftNotificationPreviewService.Object, this.appSettingsService.Object, this.localizer.Object, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);
            Action action3 = () => new DraftNotificationsController(this.notificationDataRepository.Object, this.teamDataRepository.Object, null /*draftNotificationPreviewService*/, this.appSettingsService.Object, this.localizer.Object, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);
            Action action4 = () => new DraftNotificationsController(this.notificationDataRepository.Object, this.teamDataRepository.Object, this.draftNotificationPreviewService.Object, null /*appSettingsService*/, this.localizer.Object, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);
            Action action5 = () => new DraftNotificationsController(this.notificationDataRepository.Object, this.teamDataRepository.Object, this.draftNotificationPreviewService.Object, this.appSettingsService.Object, null /*localizer*/, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);
            Action action6 = () => new DraftNotificationsController(this.notificationDataRepository.Object, this.teamDataRepository.Object, this.draftNotificationPreviewService.Object, this.appSettingsService.Object, this.localizer.Object, null/*groupsService*/, this.storageClientFactory.Object, this.userAppOptions.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("draftNotificationPreviewService is null.");
            action4.Should().Throw<ArgumentNullException>("appSettingsService is null.");
            action5.Should().Throw<ArgumentNullException>("localizer is null.");
            action6.Should().Throw<ArgumentNullException>("groupsService is null.");
        }

        /// <summary>
        /// Test case to check draftNotification handles null parameter.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_nullParam_throwsArgumentNullException()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();

            // Act
            Func<Task> task = async () => await controller.CreateDraftNotificationAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
        }

        /// <summary>
        /// Test case to validates a draft notification if Team contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_TeamSizeMorethan20_ReturnsBadRequestResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Teams = this.GetItemsList(21), Groups = new List<string>() };

            string warning = "NumberOfTeamsExceededLimitWarningFormat";
            var localizedString = new LocalizedString(warning, warning);
            this.localizer.Setup(_ => _[warning]).Returns(localizedString);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Test case to validates a draft notification if Team contains less than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_TeamSizeLessThan20_ReturnsNotificationId()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Teams = new List<string>() { "item1", "item2" }, Groups = new List<string>() };
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);

            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.Equal(((ObjectResult)result.Result).Value, this.notificationId);
        }

        /// <summary>
        /// Test case to validates a draft notification if Roaster contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_RoastersSizeMorethan20_ReturnsBadRequestResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Rosters = this.GetItemsList(21), Groups = new List<string>() };

            string warning = "NumberOfRostersExceededLimitWarningFormat";
            var localizedString = new LocalizedString(warning, warning);
            this.localizer.Setup(_ => _[warning]).Returns(localizedString);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Test case to validates a draft notification if roaster contains 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_RoastersSize20_returnsNotificationId()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Rosters = this.GetItemsList(20), Groups = new List<string>() };
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.Equal(((ObjectResult)result.Result).Value, this.notificationId);
        }

        /// <summary>
        /// Test case to validates a draft notification if Group contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_GroupSizeMorethan20_ReturnsBadRequestResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            DraftNotification notification = new DraftNotification() { Groups = this.GetItemsList(21) };

            string warning = "NumberOfGroupsExceededLimitWarningFormat";
            var localizedString = new LocalizedString(warning, warning);
            this.localizer.Setup(_ => _[warning]).Returns(localizedString);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Test case to validates a draft notification if group contains less than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_GroupSizeLessThan20_ReturnsNotificationId()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Groups = new List<string>() { "item1", "item2" } };
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.Equal(((ObjectResult)result.Result).Value, this.notificationId);
        }

        /// <summary>
        /// Test case to validates a draft notification if list has hidden membership group then return forbid.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_GrouplistHasHiddenMembership_ReturnsForbidResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(true);
            var notification = new DraftNotification() { Groups = new List<string>() };

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        /// <summary>
        /// Test case to validates a draft notification if Group list has no hidden membership group return draft Notification.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateDraft_GrouplistHasNoHiddenMembership_ReturnsOkObjectResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            var notification = new DraftNotification() { Groups = new List<string>() };
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Test to verify create draft notfication with valid data return ok result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Post_ValidData_ReturnsOkObjectResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            var notification = new DraftNotification() { Groups = new List<string>() };
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Test case to check dupliate draft Notification handles null parameter.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DuplicateDraft_nullParam_throwsArgumentNullException()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();

            // Act
            Func<Task> task = async () => await controller.DuplicateDraftNotificationAsync(null /*id*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("id is null");
        }

        /// <summary>
        /// Test case to duplicate notification for invalid data gives not found result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DuplicateNofitication_InvalidData_ReturnsNotFoundResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));
            var id = "id";

            // Act
            var result = await controller.DuplicateDraftNotificationAsync(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        /// <summary>
        /// Test case for Duplicate an existing draft notification and return Ok result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DuplicateDraft_WithExistingDraftData_ReturnOkResult()
        {
            var notificationDataEntity = new NotificationDataEntity();

            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                                      .ReturnsAsync(notificationDataEntity);
            var notificationId = "notificationId";

            string duplicate = "DuplicateText";
            var localizedString = new LocalizedString(duplicate, duplicate);
            this.localizer.Setup(_ => _[duplicate, It.IsAny<string>()]).Returns(localizedString);

            this.notificationDataRepository.Setup(x => x.DuplicateDraftNotificationAsync(It.IsAny<NotificationDataEntity>(), It.IsAny<string>()));

            // Act
            var result = await controller.DuplicateDraftNotificationAsync(notificationId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// Test case to check Update draft Notification handles null parameter.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_nullParam_throwsArgumentNullException()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();

            // Act
            Func<Task> task = async () => await controller.UpdateDraftNotificationAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
        }

        /// <summary>
        /// Test case to validates a draft notification if list has hidden membership group then return forbid.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_HiddenMembershipGroup_ReturnsForbidResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(true);
            var notification = this.GetDraftNotification();

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        /// <summary>
        /// Test case to validates a draft notification if Roaster contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_TeamSizeMoreThan20_ReturnsBadRequestResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Teams = this.GetItemsList(21), Groups = new List<string>() };

            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            string warning = "NumberOfTeamsExceededLimitWarningFormat";
            var localizedString = new LocalizedString(warning, warning);
            this.localizer.Setup(_ => _[warning]).Returns(localizedString);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        /// <summary>
        /// Test case to validates a draft notification if Team contains less than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_TeamSizeLessThan20_ReturnsNotificationId()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Teams = this.GetItemsList(10), Groups = new List<string>() };
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// Test case to validates a draft notification if Roaster contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_RoastersSizeMorethan20_ReturnsBadRequestResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Rosters = this.GetItemsList(21), Groups = new List<string>() };

            string warning = "NumberOfRostersExceededLimitWarningFormat";
            var localizedString = new LocalizedString(warning, warning);
            this.localizer.Setup(_ => _[warning]).Returns(localizedString);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        /// <summary>
        /// Test case to validates a update draft notification if roaster contains less than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_RoastersSizeLessThan20_ReturnsNotificationId()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Rosters = this.GetItemsList(10), Groups = new List<string>() };
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// Test case to validates a draft notification if Group contains more than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_GroupSizeMorethan20_ReturnsBadRequestResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            DraftNotification notification = new DraftNotification() { Groups = this.GetItemsList(21) };

            string warning = "NumberOfGroupsExceededLimitWarningFormat";
            var localizedString = new LocalizedString(warning, warning);
            this.localizer.Setup(_ => _[warning]).Returns(localizedString);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        /// <summary>
        /// Test case to validates a draft notification if group contains less than 20 items.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_GroupSizeLessThan20_ReturnsNotificationId()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = new DraftNotification() { Groups = new List<string>() { "item1", "item2" } };
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// Test case to validates a update draft should return Ok result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_ValidData_ReturnsOkResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = this.GetDraftNotification();

            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// Test case to validates a update draft should return Ok result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateDraft_CreateOrUpdateAsync_ShouldInvokedOnce()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = this.GetDraftNotification();

            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateDraftNotificationAsync(notification);

            // Assert
            this.notificationDataRepository.Verify(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>()), Times.Once());
        }

        /// <summary>
        /// Test case to check delete draft Notification by id handles null parameter.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DeleteDraft_nullParam_throwsArgumentNullException()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();

            // Act
            Func<Task> task = async () => await controller.DeleteDraftNotificationAsync(null /*id*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("id is null");
        }

        /// <summary>
        /// Test case to delete draft notification for invalid id return not found result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DeleteDraft_ForInvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = this.GetDraftNotification();

            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));

            // Act
            var result = await controller.DeleteDraftNotificationAsync(this.notificationId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        /// <summary>
        /// Test case to delete draft notification for valid id return ok result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DeleteDraft_ForValidId_ReturnsOkResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = this.GetDraftNotification();
            var notificationDataEntity = new NotificationDataEntity();

            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);
            this.notificationDataRepository.Setup(x => x.DeleteAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.DeleteDraftNotificationAsync(this.notificationId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// Test case to delete draft notification for valid id should also invoke DeleteAsync method once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Delete_CallDeleteAsync_ShouldGetInvokedOnce()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notification = this.GetDraftNotification();
            var notificationDataEntity = new NotificationDataEntity();

            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);
            this.notificationDataRepository.Setup(x => x.DeleteAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.DeleteDraftNotificationAsync(this.notificationId);

            // Assert
            this.notificationDataRepository.Verify(x => x.DeleteAsync(It.IsAny<NotificationDataEntity>()), Times.Once());
        }

        /// <summary>
        /// Test case to get all draft notification summary.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_AllDraftSummary_ReturnsDraftSummaryList()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notificationEntityList = new List<NotificationDataEntity>() { new NotificationDataEntity() { Id = "notificationId", Title = "notificationTitle" } };
            var notificationEntity = notificationEntityList.FirstOrDefault();
            this.notificationDataRepository.Setup(x => x.GetAllDraftNotificationsAsync()).ReturnsAsync(notificationEntityList);

            // Act
            var result = await controller.GetAllDraftNotificationsAsync();
            var allDraftNotificationSummary = (List<DraftNotificationSummary>)result.Value;

            // Assert
            Assert.IsType<List<DraftNotificationSummary>>(allDraftNotificationSummary);
        }

        /// <summary>
        /// Test case to check mapping of draft notification summary response.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CorrectMapping_ReturnsDraftNotificationSummary()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notificationEntityList = new List<NotificationDataEntity>() { new NotificationDataEntity() { Id = "notificationId", Title = "notificationTitle" } };
            var notificationEntity = notificationEntityList.FirstOrDefault();
            this.notificationDataRepository.Setup(x => x.GetAllDraftNotificationsAsync()).ReturnsAsync(notificationEntityList);

            // Act
            var result = await controller.GetAllDraftNotificationsAsync();
            var draftNotificationSummaryList = (List<DraftNotificationSummary>)result.Value;
            var draftNotificationSummary = draftNotificationSummaryList.FirstOrDefault();

            // Assert
            Assert.Equal(draftNotificationSummary.Id, notificationEntity.Id);
            Assert.Equal(draftNotificationSummary.Title, notificationEntity.Title);
        }

        /// <summary>
        /// Test case to check get draft Notification by id handles null parameter.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetDraft_nullParam_throwsArgumentNullException()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();

            // Act
            Func<Task> task = async () => await controller.GetDraftNotificationByIdAsync(null /*id*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("id is null");
        }

        /// <summary>
        /// Test to check a draft notification by Id.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_DraftNotificationById_ReturnsOkResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notificationDataEntity = new NotificationDataEntity()
            {
                TeamsInString = "['team1','team2']",
                RostersInString = "['roster1','roster2']",
                GroupsInString = "['group1','group2']",
            };

            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);

            // Act
            var result = await controller.GetDraftNotificationByIdAsync(this.notificationId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Test to check a draft notification by Id.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetDraft_WithEmptyGroupsInString_ReturnsOkResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notificationDataEntity = new NotificationDataEntity()
            {
                TeamsInString = "['team1','team2']",
                RostersInString = "['roster1','roster2']",
                GroupsInString = null,
            };

            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);

            // Act
            var result = await controller.GetDraftNotificationByIdAsync(this.notificationId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Test to check a draft notification by Id.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_InvalidInputId_ReturnsNotFoundResult()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));
            var id = "invalidId";

            // Act
            var result = await controller.GetDraftNotificationByIdAsync(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        /// <summary>
        /// Test case to check get draft Notification by id handles null parameter.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetDraftSummaryConsent_nullParam_throwsArgumentNullException()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();

            // Act
            Func<Task> task = async () => await controller.GetDraftNotificationSummaryForConsentByIdAsync(null /*notificationId*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
        }

        /// <summary>
        /// Test to check a draft notification summary consent page for invalid data return not found.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetDraftSummaryConsent_ForInvalidData_ReturnsNotFound()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));

            // Act
            var result = await controller.GetDraftNotificationSummaryForConsentByIdAsync(this.notificationId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        /// <summary>
        /// Test to check mapping of the draftNotificationSummaryConsent.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CorrectMapping_ReturnsDraftNotificationSummaryForConsentList()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notificationDataEntity = new NotificationDataEntity()
            {
                TeamsInString = "['data1','data1']",
                RostersInString = "['data1','data1']",
                GroupsInString = "['group1','group2']",
                AllUsers = true,
            };
            var groupList = new List<Group>() { new Group() { Id = "Id1", DisplayName = "group1" }, new Group() { Id = "Id2", DisplayName = "group2" } };
            var teams = new List<string>() { "data1", "data1" };
            var rosters = new List<string>() { "data1", "data1" };
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);
            this.groupsService.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).Returns(groupList.ToAsyncEnumerable());
            this.teamDataRepository.Setup(x => x.GetTeamNamesByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(teams);
            this.teamDataRepository.Setup(x => x.GetTeamNamesByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(rosters);

            // Act
            var result = await controller.GetDraftNotificationSummaryForConsentByIdAsync(this.notificationId);
            var draftNotificationSummaryConsent = (DraftNotificationSummaryForConsent)((ObjectResult)result.Result).Value;

            // Assert
            Assert.Equal(draftNotificationSummaryConsent.NotificationId, this.notificationId);
            Assert.Equal(draftNotificationSummaryConsent.TeamNames, teams);
            Assert.Equal(draftNotificationSummaryConsent.RosterNames, rosters);
            Assert.Equal(draftNotificationSummaryConsent.AllUsers, notificationDataEntity.AllUsers);
            Assert.Equal(draftNotificationSummaryConsent.GroupNames, notificationDataEntity.Groups);
        }

        /// <summary>
        /// Test to check a draft notification summary consent page for invalid data return not found.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetDraftSummaryConsent_ForValidData_ReturnsDraftSummaryForConsent()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var notificationDataEntity = new NotificationDataEntity()
            {
                TeamsInString = "['team1','team2']",
                RostersInString = "['roster1','roster2']",
                GroupsInString = "['group1','group2']",
                AllUsers = true,
            };
            var groupList = new List<Group>() { new Group() };
            var teams = new List<string>() { "team1", "team2" };
            var rosters = new List<string>() { "roster1", "roster2" };
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDataEntity);
            this.groupsService.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).Returns(groupList.ToAsyncEnumerable());
            this.teamDataRepository.Setup(x => x.GetTeamNamesByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(teams);
            this.teamDataRepository.Setup(x => x.GetTeamNamesByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(rosters);

            // Act
            var result = await controller.GetDraftNotificationSummaryForConsentByIdAsync(this.notificationId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Test case verify preview draft notification for invalid input returns badrequest.
        /// </summary>
        /// <param name="draftNotificationPreviewRequest">draftNotificationPreviewRequest.</param>
        /// <param name="draftNotificationId">draft Notification Id.</param>
        /// <param name="teamsTeamId">teams TeamId.</param>
        /// <param name="teamsChannelId">teams ChannelId.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Theory]
        [MemberData(nameof(DraftPreviewPrams))]
        public async Task PreviewDraft_InvalidInput_ReturnsBadRequest(DraftNotificationPreviewRequest draftNotificationPreviewRequest, string draftNotificationId, string teamsTeamId, string teamsChannelId)
        {
            // Arrange
            if (draftNotificationPreviewRequest != null)
            {
                draftNotificationPreviewRequest.DraftNotificationId = draftNotificationId;
                draftNotificationPreviewRequest.TeamsTeamId = teamsTeamId;
                draftNotificationPreviewRequest.TeamsChannelId = teamsChannelId;
            }

            var controller = this.GetDraftNotificationsController();

            // Act
            var result = await controller.PreviewDraftNotificationAsync(draftNotificationPreviewRequest);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        /// <summary>
        /// Test to check preview draft for invalid draftNotificationId.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PreviewDraft_InvalidNotificationId_ReturnsBadRequest()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var draftNotificationPreviewRequest = this.GetdraftNotificationPreviewRequest();
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));

            // Act
            var result = await controller.PreviewDraftNotificationAsync(draftNotificationPreviewRequest);
            var errorMessage = ((ObjectResult)result).Value;

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, $"Notification {draftNotificationPreviewRequest.DraftNotificationId} not found.");
        }

        /// <summary>
        /// Test case for preview Draft notification for valid input returns status code 200 OK.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PreviewDraft_ValidInput_ReturnsStatusCode200OK()
        {
            // Arrange
            var controller = this.GetDraftNotificationsController();
            var draftNotificationPreviewRequest = this.GetdraftNotificationPreviewRequest();
            var notificationDatEntity = new NotificationDataEntity();
            var httpStatusCodeOk = 200;
            this.notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificationDatEntity);
            this.appSettingsService.Setup(x => x.GetServiceUrlAsync()).ReturnsAsync("ServiceUrl");
            this.draftNotificationPreviewService.Setup(x => x.SendPreview(It.IsAny<NotificationDataEntity>(), It.IsAny<TeamDataEntity>(), It.IsAny<string>())).ReturnsAsync(HttpStatusCode.OK);

            // Act
            var result = await controller.PreviewDraftNotificationAsync(draftNotificationPreviewRequest);
            var statusCode = ((StatusCodeResult)result).StatusCode;

            // Assert
            Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(statusCode, httpStatusCodeOk);
        }

        /// <summary>
        /// Test to verify create draft notfication with valid data return ok result.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Upload_Image_Blob_Storage_ReturnsOkObjectResult()
        {
            // Arrange
            this.imageUploadBlobStorage = true;
            var controller = this.GetDraftNotificationsController();
            this.groupsService.Setup(x => x.ContainsHiddenMembershipAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);
            var notification = new DraftNotification() { Groups = new List<string>(), ImageLink = "data:image/jpeg;base64,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" };
            this.notificationDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(x => x.UploadAsync(It.IsAny<MemoryStream>(), true, It.IsAny<CancellationToken>()));
            mockBlobClient.Setup(x => x.GenerateSasUri(It.IsAny<BlobSasBuilder>())).Returns(new Uri("http://demo.com"));
            
            var mockContainerClient = new Mock<BlobContainerClient>();
            mockContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<CancellationToken>()));
            mockContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockContainerClient
                .Setup(x => x.CanGenerateSasUri).Returns(true);

            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(It.IsAny<string>()))
                .Returns(mockContainerClient.Object);

            // Act
            var result = await controller.CreateDraftNotificationAsync(notification);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftNotificationsController"/> class.
        /// </summary>
        private DraftNotificationsController GetDraftNotificationsController()
        {
            this.userAppOptions.Setup(x => x.Value).Returns(new UserAppOptions(){ImageUploadBlobStorage = this.imageUploadBlobStorage});
            this.notificationDataRepository.Setup(x => x.TableRowKeyGenerator).Returns(new TableRowKeyGenerator());
            this.notificationDataRepository.Setup(x => x.TableRowKeyGenerator.CreateNewKeyOrderingOldestToMostRecent()).Returns(this.notificationId);
            this.userAppOptions.Setup(x => x.Value).Returns(new UserAppOptions() { MaxNumberOfTeams = 20 });
            var controller = new DraftNotificationsController(this.notificationDataRepository.Object, this.teamDataRepository.Object, this.draftNotificationPreviewService.Object, this.appSettingsService.Object, this.localizer.Object, this.groupsService.Object, this.storageClientFactory.Object, this.userAppOptions.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            return controller;
        }

        private DraftNotification GetDraftNotification()
        {
            return new DraftNotification() { Groups = new List<string>() };
        }

        private List<string> GetItemsList(int itemCount)
        {
            var itemList = new List<string>();
            for (int item = 1; item <= itemCount; item++)
            {
                itemList.Add("item" + item);
            }

            return itemList;
        }

        private DraftNotificationPreviewRequest GetdraftNotificationPreviewRequest()
        {
            return new DraftNotificationPreviewRequest()
            {
                DraftNotificationId = "draftNotificationId",
                TeamsChannelId = "teamsChannelId",
                TeamsTeamId = "teamsTeamId",
            };
        }
    }
}
