// <copyright file="FunctionNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using FluentAssertions;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
        string teamId = "121";
        string notificationId = "111";

        /// <summary>
        /// Constructor tests.
        /// </summary> 
        [Fact]
        public void SyncTeamMembersActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new SyncTeamMembersActivity(null /*teamDataRepository*/, membersService.Object, notificationDataRepository.Object, sentNotificationDataRepository.Object, localier.Object, userDataRepository.Object);
            Action action2 = () => new SyncTeamMembersActivity(teamDataRepository.Object, null /*membersService*/, notificationDataRepository.Object, sentNotificationDataRepository.Object, localier.Object, userDataRepository.Object);
            Action action3 = () => new SyncTeamMembersActivity(teamDataRepository.Object, membersService.Object, null /*notificationDataRepository*/, sentNotificationDataRepository.Object, localier.Object, userDataRepository.Object);
            Action action4 = () => new SyncTeamMembersActivity(teamDataRepository.Object, membersService.Object, notificationDataRepository.Object, null /*sentNotificationDataRepository*/, localier.Object, userDataRepository.Object);
            Action action5 = () => new SyncTeamMembersActivity(teamDataRepository.Object, membersService.Object, notificationDataRepository.Object, sentNotificationDataRepository.Object, null /*localier*/, userDataRepository.Object);
            Action action6 = () => new SyncTeamMembersActivity(teamDataRepository.Object, membersService.Object, notificationDataRepository.Object, sentNotificationDataRepository.Object, localier.Object, null /*userDataRepository*/);
            Action action7 = () => new SyncTeamMembersActivity(teamDataRepository.Object, membersService.Object, notificationDataRepository.Object, sentNotificationDataRepository.Object, localier.Object, userDataRepository.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("membersService is null.");
            action3.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action4.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("localier is null.");
            action5.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action7.Should().NotThrow();
        }

        /// <summary>
        /// Success test for syncs team members to repository.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task SyncTeamMembersActivitySuccessTest()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            TeamDataEntity teamData = new TeamDataEntity() {TenantId ="Tanant1", TeamId = "Team1", ServiceUrl ="serviceUrl" };
            IEnumerable<UserDataEntity> userData = new List<UserDataEntity>()
            {
                new UserDataEntity()
            };
            teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(teamData);
            membersService
                .Setup(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(userData);
            sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, teamId), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            teamDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals(teamId))));
            membersService.Verify(x => x.GetUsersAsync(It.Is<string>(x => x.Equals(teamData.TeamId)), It.Is<string>(x => x.Equals(teamData.TenantId)), It.Is<string>(x => x.Equals(teamData.ServiceUrl))));
        }

        /// <summary>
        /// Test for team Members info not found scenario.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task SyncTeamMemberActivity_TeamInfoNotFoundTest()
        {
            // Arrange
            var activityContext = this.GetSyncTeamMembersActivity();
            
            // teamInfo is null
            teamDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(TeamDataEntity)));
            notificationDataRepository
                .Setup(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, teamId), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x=>x.SaveWarningInNotificationDataEntityAsync(It.Is<string>(x=>x.Equals(notificationId)), It.IsAny<string>()));
        }

        /// <summary>
        /// ArgumentNullException Test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task ArgumentNullExceptionTest()
        {
            // Arrange
            var teamId = "team1";
            var notificationId = "notificationId";
            var activityContext = this.GetSyncTeamMembersActivity();

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((null /*notificationId*/, teamId), logger.Object);
            Func<Task> task1 = async () => await activityContext.RunAsync((notificationId, null /*teamId*/), logger.Object);
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
            return new SyncTeamMembersActivity(teamDataRepository.Object, membersService.Object, notificationDataRepository.Object, sentNotificationDataRepository.Object, localier.Object, userDataRepository.Object);
        }
    }
}
