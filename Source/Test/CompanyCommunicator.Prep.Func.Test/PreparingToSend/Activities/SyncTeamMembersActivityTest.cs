// <copyright file="SyncTeamMembersActivityTest.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SyncTeamMembersActivity test class.
    /// </summary>
    public class SyncTeamMembersActivityTest
    {
        private readonly Mock<ITeamMembersService> membersService = new Mock<ITeamMembersService>();
        private readonly Mock<IStringLocalizer<Strings>> localier = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        private readonly Mock<IUserTypeService> userTypeService = new Mock<IUserTypeService>();
        private readonly string teamId = "121";
        private readonly string notificationId = "111";

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void SyncTeamMembersActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new SyncTeamMembersActivity(null /*teamDataRepository*/, this.membersService.Object, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.userDataRepository.Object, this.userTypeService.Object);
            Action action2 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, null /*membersService*/, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.userDataRepository.Object, this.userTypeService.Object);
            Action action3 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, null /*notificationDataRepository*/, this.sentNotificationDataRepository.Object, this.localier.Object, this.userDataRepository.Object, this.userTypeService.Object);
            Action action4 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, this.notificationDataRepository.Object, null /*sentNotificationDataRepository*/, this.localier.Object, this.userDataRepository.Object, this.userTypeService.Object);
            Action action5 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, null /*localier*/, this.userDataRepository.Object, this.userTypeService.Object);
            Action action6 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, null /*userDataRepository*/, this.userTypeService.Object);
            Action action7 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.userDataRepository.Object, null /*userTypeService*/);
            Action action8 = () => new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.userDataRepository.Object, this.userTypeService.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("membersService is null.");
            action3.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action4.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("localier is null.");
            action6.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action6.Should().Throw<ArgumentNullException>("userTypeService is null.");
            action8.Should().NotThrow();
        }

        /// <summary>
        /// Test case to verify that existing Member users is stored in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_OnlyExistingMemberUser_StoreInSentNotificationTable()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            var userDataList = new List<UserDataEntity>()
            {
                new UserDataEntity() { UserId = "userId", UserType = UserType.Member },
                new UserDataEntity() { UserId = "userId", UserType = UserType.Member },
            };
            var userData = new UserDataEntity() { UserId = "userId" };
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataList);
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify that existing Guest Users gets saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_OnlyExistingGuestUser_StoreInSentNotificationTable()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            var userDataList = new List<UserDataEntity>()
            {
                new UserDataEntity() { UserId = "userId", UserType = UserType.Guest },
            };
            var userData = new UserDataEntity() { UserId = "userId", UserType = UserType.Guest };
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataList);
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify that both user type i.e. existing Member user and Guest user
        /// is saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_BothUserTypeForExistingUser_ShouldStoreBothUserType()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            var userDataList = new List<UserDataEntity>()
            {
                new UserDataEntity() { UserId = "userId1", UserType = UserType.Guest },
                new UserDataEntity() { UserId = "userId2", UserType = UserType.Member },
            };
            var userData = new UserDataEntity() { UserId = "userId1" };
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataList);
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(l => l.Count() == 2)), Times.Once);
        }

        /// <summary>
        /// Test case to verify that new Member users is stored in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_OnlyNewMemberUser_StoreInSentNotificationTable()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            var userDataList = new List<UserDataEntity>()
            {
                new UserDataEntity() { UserId = "userId", UserType = UserType.Member },
                new UserDataEntity() { UserId = "userId", UserType = UserType.Member },
            };
            UserDataEntity userData = null;
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataList);
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify that existing Guest Users never gets saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_OnlyNewGuestUser_NeverStoreInSentNotificationTable()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            var userDataList = new List<UserDataEntity>()
            {
                new UserDataEntity() { UserId = "userId", UserType = UserType.Guest },
            };
            UserDataEntity userData = null;
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataList);
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(x => x.Count() == 0)), Times.Once);
        }

        /// <summary>
        /// Test case to verify that only Member user type is filtered from list of new Member user and Guest user,
        /// and is saved in Sent Notification Table.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_BothUserTypeForNewUser_StoreOnlyMemberUserType()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            var userDataList = new List<UserDataEntity>()
            {
                new UserDataEntity() { UserId = "userId1", UserType = UserType.Guest },
                new UserDataEntity() { UserId = "userId2", UserType = UserType.Member },
            };
            UserDataEntity userData = null;
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataList);
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);

            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(l => l.Count() == 1)), Times.Once);
        }

        /// <summary>
        /// Test for team Members info not found scenario.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMemberActivity_TeamInfoNotFoundTest()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();

            // teamInfo is null
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(TeamDataEntity)));
            this.notificationDataRepository
                .Setup(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.notificationDataRepository.Verify(x => x.SaveWarningInNotificationDataEntityAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<string>()));
        }

        /// <summary>
        /// Test case to check if exception is caught and logged in case graph returns null reponse.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamMembers_NullResponseFromBotAPI_CatchExceptionAndLog()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            var teamData = new TeamDataEntity() { TenantId = "Tanant1", TeamId = "Team1", ServiceUrl = "serviceUrl" };
            IEnumerable<UserDataEntity> userDataEntities = null;
            this.teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            this.membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userDataEntities);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((this.notificationId, this.teamId), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.notificationDataRepository.Verify(x => x.SaveWarningInNotificationDataEntityAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// ArgumentNullException Test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ArgumentNullExceptionTest()
        {
            // Arrange
            var teamId = "team1";
            var notificationId = "notificationId";
            var activityContext = this.GetSyncTeamMembersActivity();

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((null /*notificationId*/, teamId), this.logger.Object);
            Func<Task> task1 = async () => await activityContext.RunAsync((notificationId, null /*teamId*/), this.logger.Object);
            Func<Task> task2 = async () => await activityContext.RunAsync((notificationId, teamId), null /*logger*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
            await task1.Should().ThrowAsync<ArgumentNullException>("teamId is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("logger is null");
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="SyncTeamMembersActivity"/> class.
        /// </summary>
        private SyncTeamMembersActivity GetSyncTeamMembersActivity()
        {
            return new SyncTeamMembersActivity(this.teamDataRepository.Object, this.membersService.Object, this.notificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.userDataRepository.Object, this.userTypeService.Object);
        }
    }
}
