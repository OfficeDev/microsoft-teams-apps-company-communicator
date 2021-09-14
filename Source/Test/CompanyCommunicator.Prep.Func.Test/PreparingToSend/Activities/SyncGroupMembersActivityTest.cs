// <copyright file="SyncGroupMembersActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SyncGroupMembersActivity test class.
    /// </summary>
    public class SyncGroupMembersActivityTest
    {
        private readonly Mock<IGroupMembersService> groupMembersService = new Mock<IGroupMembersService>();
        private readonly Mock<IStringLocalizer<Strings>> localier = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<IUserTypeService> userTypeService = new Mock<IUserTypeService>();

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void ConstructorArgumentNullException_Test()
        {
            // Arrange
            Action action1 = () => new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, this.notificationDataRepository.Object, this.groupMembersService.Object, null /*userDataRepository*/, this.userTypeService.Object, this.localier.Object);
            Action action2 = () => new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, this.notificationDataRepository.Object, this.groupMembersService.Object, this.userDataRepository.Object, this.userTypeService.Object, null /*localier*/);
            Action action3 = () => new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, this.notificationDataRepository.Object, null /*groupMembersService*/, this.userDataRepository.Object, this.userTypeService.Object, this.localier.Object);
            Action action4 = () => new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, null /*notificationDataRepository*/, this.groupMembersService.Object, this.userDataRepository.Object, this.userTypeService.Object, this.localier.Object);
            Action action5 = () => new SyncGroupMembersActivity(null /*sentNotificationDataRepository*/, this.notificationDataRepository.Object, this.groupMembersService.Object, this.userDataRepository.Object, this.userTypeService.Object, this.localier.Object);
            Action action6 = () => new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, this.notificationDataRepository.Object, this.groupMembersService.Object, this.userDataRepository.Object, this.userTypeService.Object, this.localier.Object);
            Action action7 = () => new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, this.notificationDataRepository.Object, this.groupMembersService.Object, this.userDataRepository.Object, null /*userTypeService*/, this.localier.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("localier is null.");
            action3.Should().Throw<ArgumentNullException>("groupMembersService is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action6.Should().NotThrow();
            action7.Should().Throw<ArgumentNullException>("userTypeService is null.");
        }

        /// <summary>
        /// Test case to verify that new Member users is stored in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_OnlyMemberNewUserType_StoreInSentNotificationTable()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User() { Id = "userId", UserPrincipalName = "userPrincipalName" },
            };
            UserDataEntity userDataEntity = null;

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntity);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserAsync(It.IsAny<UserDataEntity>(), It.IsAny<string>()));

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(x => x.FirstOrDefault().PartitionKey == notificationId)));
        }

        /// <summary>
        /// Test case to verify that new Guest Users never gets saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_OnlyGuestNewUsersType_NeverStoreInSentNotificationTable()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User() { Id = "userId", UserPrincipalName = "#ext#" },
            };
            UserDataEntity userDataEntity = null;

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntity);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserAsync(It.IsAny<UserDataEntity>(), It.IsAny<string>()));

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(x => x.Count() == 0)), Times.Once);
        }

        /// <summary>
        /// Test case to verify that only Member user type is filtered from list of new Member user and Guest user,
        /// and is saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_BothUserTypeForNewUser_StoreOnlyMemberUserType()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User() { Id = "userId1", UserPrincipalName = "userPrincipalName1" },
                new User() { Id = "userId2", UserPrincipalName = "#ext#" },
            };
            UserDataEntity userDataEntity = null;

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntity);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserAsync(It.IsAny<UserDataEntity>(), It.IsAny<string>()));

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(l => l.Count() == 1)), Times.Once);
        }

        /// <summary>
        /// Test case to verify that existing Member users is stored in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_OnlyMemberExistingUserType_StoreInSentNotificationTable()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User() { Id = "userId", UserPrincipalName = "userPrincipalName" },
            };
            var userDataEntity = new UserDataEntity()
            {
                UserId = "userId",
            };

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntity);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserAsync(It.IsAny<UserDataEntity>(), It.IsAny<string>()));

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(x => x.FirstOrDefault().PartitionKey == notificationId)));
        }

        /// <summary>
        /// Test case to verify that existing Guest Users gets saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_OnlyGuestExistingUsersType_ShouldStoreInSentNotificationTable()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User() { Id = "userId", UserPrincipalName = "#ext#" },
            };
            var userDataEntity = new UserDataEntity()
            {
                UserId = "userId",
            };

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntity);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserAsync(It.IsAny<UserDataEntity>(), It.IsAny<string>()));

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify that both existing user is saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_BothUserTypeForExistingUser_ShouldStoreInSentNotificationTable()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User() { Id = "userId1", UserPrincipalName = "userPrincipalName1" },
                new User() { Id = "userId2", UserPrincipalName = "#ext#" },
            };
            var userDataEntity = new UserDataEntity()
            {
                UserId = "userId1",
            };

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntity);
            this.userTypeService
                .Setup(x => x.UpdateUserTypeForExistingUserAsync(It.IsAny<UserDataEntity>(), It.IsAny<string>()));

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(l => l.Count() == 2)), Times.Once);
        }

        /// <summary>
        /// Test case to check if exception is caught and logged in case graph returns null reponse.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_NullResponseFromGraph_CatchExceptionAndLog()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            List<User> users = null;
            var userDataEntity = new UserDataEntity()
            {
                UserId = "userId",
            };

            this.groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.notificationDataRepository.Verify(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case to check ArgumentNullException is thrown when parameter is null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncGroupMembers_NullParameter_ShouldThrowException()
        {
            // Arrange
            var groupId = "GroupId";
            var notificationId = "noticationId";
            var activityContext = this.GetSyncGroupMembersActivity();

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((null /*notificationId*/, groupId), this.logger.Object);
            Func<Task> task1 = async () => await activityContext.RunAsync((notificationId, null /*groupId*/), this.logger.Object);
            Func<Task> task2 = async () => await activityContext.RunAsync((notificationId, groupId), null /*logger*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
            await task1.Should().ThrowAsync<ArgumentNullException>("groupId is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("logger is null");
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="SyncGroupMembersActivity"/> class.
        /// </summary>
        private SyncGroupMembersActivity GetSyncGroupMembersActivity()
        {
            return new SyncGroupMembersActivity(this.sentNotificationDataRepository.Object, this.notificationDataRepository.Object, this.groupMembersService.Object, this.userDataRepository.Object, this.userTypeService.Object, this.localier.Object);
        }
    }
}
