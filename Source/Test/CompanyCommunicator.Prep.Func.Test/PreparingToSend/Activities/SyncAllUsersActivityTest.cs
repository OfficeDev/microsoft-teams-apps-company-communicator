// <copyright file="SyncAllUsersActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SyncAllUsersActivity test class.
    /// </summary>
    public class SyncAllUsersActivityTest
    {
        private readonly Mock<IUsersService> userService = new Mock<IUsersService>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<IUserTypeService> userTypeService = new Mock<IUserTypeService>();
        private readonly Mock<IRecipientsService> recipientsService = new Mock<IRecipientsService>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void SyncAllUsersActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new SyncAllUsersActivity(null /*userDataRepository*/, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.userTypeService.Object, this.recipientsService.Object, this.localizer.Object);
            Action action2 = () => new SyncAllUsersActivity(this.userDataRepository.Object, null /*sentNotificationDataRepository*/, this.userService.Object, this.notificationDataRepository.Object, this.userTypeService.Object, this.recipientsService.Object, this.localizer.Object);
            Action action3 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, null /*userService*/, this.notificationDataRepository.Object, this.userTypeService.Object, this.recipientsService.Object, this.localizer.Object);
            Action action4 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, null /*notificationDataRepository*/, this.userTypeService.Object, this.recipientsService.Object, this.localizer.Object);
            Action action5 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, null /*userTypeService*/, this.recipientsService.Object, this.localizer.Object);
            Action action6 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.userTypeService.Object, null /*recipientsService*/, this.localizer.Object);
            Action action7 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.userTypeService.Object, this.recipientsService.Object, null /*localizer*/);
            Action action8 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.userTypeService.Object, this.recipientsService.Object, this.localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("userService is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("userTypeService is null.");
            action6.Should().Throw<ArgumentNullException>("recipientsService is null.");
            action7.Should().Throw<ArgumentNullException>("localizer is null.");
            action8.Should().NotThrow();
        }

        /// <summary>
        /// Test case to verify all member type users gets stored in sentNotification table and also, get saved as partitions.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_OnlyMemberTypeUsers_ShouldBeSavedInSentNotificationTable()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> userDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = "user1", UserType = UserType.Member },
               new UserDataEntity() { Name = "user2", UserType = UserType.Member },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "101", UserType = UserType.Member } }, deltaLink);
            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);
            this.userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userDataResponse);
            this.userService
                .Setup(x => x.HasTeamsLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserListAsync(It.IsAny<IEnumerable<UserDataEntity>>()))
                .Returns(Task.CompletedTask);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => x.RowKey == tuple.Item1.FirstOrDefault().Id)), Times.AtLeastOnce);
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(l => l.Count() == 2)), Times.Once);
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify guest users from Graph service does not get saved in UserData table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_GuestUsersFromGraph_ShouldNotBeSavedInTable()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> userDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = "user1", UserType = UserType.Member },
               new UserDataEntity() { Name = "user2", UserType = UserType.Guest },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "101", UserType = "Guest" } }, deltaLink);
            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);
            this.userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userDataResponse);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserListAsync(It.IsAny<IEnumerable<UserDataEntity>>()))
                .Returns(Task.CompletedTask);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        /// <summary>
        /// Test case to verify existing guest users gets stored in sentNotificatinData table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_AllGuestUsersFromDB_ShouldBeSavedInTable()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> userDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = "user1", UserType = UserType.Member },
               new UserDataEntity() { Name = "user2", UserType = UserType.Guest },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "101", UserType = "Guest" } }, deltaLink);
            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);
            this.userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userDataResponse);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserListAsync(It.IsAny<IEnumerable<UserDataEntity>>()))
                .Returns(Task.CompletedTask);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(l => l.Count() == 2)), Times.Once);
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify that no exception is thrown when there is no users in db.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_NullUsersFromDB_ShouldNotThrowException()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> userDataResponse = null;
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "101", UserType = "Guest" } }, deltaLink);
            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);
            this.userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userDataResponse);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserListAsync(It.IsAny<IEnumerable<UserDataEntity>>()))
                .Returns(Task.CompletedTask);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Never);
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Never);
        }

        /// <summary>
        /// Test case to verify when deltalink is expired InvalidaOperationException is thrown to call SyncAllUsers again.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_ExpiredDeltaLink_ThrowsInvalidOperationException()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "expiredDeltaLink";
            IEnumerable<UserDataEntity> userDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = string.Empty },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userDataResponse);

            var serviceException = new ServiceException(null, null, HttpStatusCode.BadRequest);
            this.userService.Setup(x => x.GetAllUsersAsync(It.IsAny<string>())).ThrowsAsync(serviceException);

            Func<Task> task = async () => await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            await task.Should().ThrowAsync<InvalidOperationException>();
            this.userService.Verify(x => x.GetAllUsersAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        /// <summary>
        /// Test method to verify that removed users as per deltalink from graph
        /// should be deleted from User table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUser_RemovedUsers_DeleteUserFromTable()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> userDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = string.Empty, UserType = UserType.Member },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            var userData = new UserDataEntity() { AadId = "101" };

            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "101", AdditionalData = new Dictionary<string, object>() { { "@removed", null } } } }, deltaLink);

            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);
            this.userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userDataResponse);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.userDataRepository
                .Setup(x => x.DeleteAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
            this.userDataRepository.Verify(x => x.DeleteAsync(It.IsAny<UserDataEntity>()), Times.Once);
        }

        /// <summary>
        /// Test method to verify users with no team license will not get saved in UserData table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_NoTeamsLicense_NeverGetSavedInTable()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            var userList = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = "user1", UserType = UserType.Guest },
            };
            var notification = new NotificationDataEntity() { Id = "notificationId1" };
            var tuple = (new List<User>() { new User() { Id = "101", UserType = UserType.Member } }, string.Empty);

            this.userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(string.Empty);
            this.userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);

            this.userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(userList);

            this.userService
                .Setup(x => x.HasTeamsLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            await activityContext.RunAsync(notification, this.logger.Object);

            // Assert
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => x.RowKey == tuple.Item1.FirstOrDefault().Id)), Times.Never);
        }

        /// <summary>
        /// Test case to check ArgumentNullException when parameter is null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsers_NullParameter_ShouldThrowException()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(null /*notification*/, this.logger.Object);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="SyncAllUsersActivity"/> class.
        /// </summary>
        private SyncAllUsersActivity GetSyncAllUsersActivity()
        {
            return new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.userTypeService.Object, this.recipientsService.Object, this.localizer.Object);
        }
    }
}
