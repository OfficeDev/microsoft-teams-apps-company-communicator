// <copyright file="SyncTeamsActivityTest.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SyncTeamsActivity test class.
    /// </summary>
    public class SyncTeamsActivityTest
    {
        private readonly Mock<IStringLocalizer<Strings>> localier = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> log = new Mock<ILogger>();
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        private readonly Mock<IRecipientsService> recipientsService = new Mock<IRecipientsService>();

        /// <summary>
        /// Constructor test.
        /// </summary>
        [Fact]
        public void SyncTeamsActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new SyncTeamsActivity(null /*teamDataRepository*/, this.sentNotificationDataRepository.Object, this.localier.Object, this.notificationDataRepository.Object, this.recipientsService.Object);
            Action action2 = () => new SyncTeamsActivity(this.teamDataRepository.Object, null /*sentNotificationDataRepository*/, this.localier.Object, this.notificationDataRepository.Object, this.recipientsService.Object);
            Action action3 = () => new SyncTeamsActivity(this.teamDataRepository.Object, this.sentNotificationDataRepository.Object, null /*localizer*/, this.notificationDataRepository.Object, this.recipientsService.Object);
            Action action4 = () => new SyncTeamsActivity(this.teamDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, null /*notificationDataRepository*/, this.recipientsService.Object);
            Action action5 = () => new SyncTeamsActivity(this.teamDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.notificationDataRepository.Object, this.recipientsService.Object);
            Action action6 = () => new SyncTeamsActivity(this.teamDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.notificationDataRepository.Object, null /*recipientsService*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("localizer is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action5.Should().NotThrow();
            action4.Should().Throw<ArgumentNullException>("recipientsService is null.");
        }

        /// <summary>
        /// Sync Teams activity success test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamsActivitySuccessTest()
        {
            // Arrange
            var activityContext = this.GetSyncTamActivity();
            IEnumerable<string> roasters = new List<string>() { "teamId1", "teamId2" };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId",
                Rosters = roasters,
                TeamsInString = "['teamId1','teamId2']",
            };

            IEnumerable<TeamDataEntity> teamData = new List<TeamDataEntity>()
            {
                new TeamDataEntity() { TeamId = "teamId1" },
                new TeamDataEntity() { TeamId = "teamId2" },
            };

            this.teamDataRepository
                .Setup(x => x.GetTeamDataEntitiesByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(teamData);
            this.notificationDataRepository
                .Setup(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            RecipientsInfo recipientsInfo = default;
            Func<Task> task = async () =>
            {
                recipientsInfo = await activityContext.RunAsync(notification, this.log.Object);
            };

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(
                x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(
                x => x.Count() == 2)), Times.Once);
            this.notificationDataRepository.Verify(x => x.SaveWarningInNotificationDataEntityAsync(It.Is<string>(x => x.Equals(notification.Id)), It.IsAny<string>()), Times.Never());
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Sync teams data to Sent notification repository. Save warning message logged for each team that is absent in repository.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamsActivitySuccessWithSaveWarningNotificationTest()
        {
            // Arrange
            var activityContext = this.GetSyncTamActivity();
            IEnumerable<string> roasters = new List<string>() { "teamId1", "teamId2" };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "123",
                Rosters = roasters,
                TeamsInString = "['teamId1','teamId2']",
            };
            IEnumerable<TeamDataEntity> teamData = new List<TeamDataEntity>()
            {
                new TeamDataEntity() { TeamId = "teamId1" },
            };

            this.teamDataRepository
                .Setup(x => x.GetTeamDataEntitiesByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(teamData);
            this.notificationDataRepository
                .Setup(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(notification, this.log.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(x => x.Count() == 1)));
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);

            // Warn message should be logged once for "teamId2".
            this.notificationDataRepository.Verify(x => x.SaveWarningInNotificationDataEntityAsync(It.Is<string>(x => x.Equals(notification.Id)), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// SyncTeamsActivity argumentNullException test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncTeamsActivityNullArgumentTest()
        {
            // Arrange
            var activityContext = this.GetSyncTamActivity();
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId",
            };
            IEnumerable<TeamDataEntity> teamData = new List<TeamDataEntity>();
            this.teamDataRepository.Setup(x => x.GetTeamDataEntitiesByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(teamData);

            // Act
            Func<Task> task1 = async () => await activityContext.RunAsync(null /*notification*/, null/*logger*/);
            Func<Task> task2 = async () => await activityContext.RunAsync(null /*notification*/, this.log.Object);
            Func<Task> task3 = async () => await activityContext.RunAsync(notification, null /*logger*/);

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>();
            await task2.Should().ThrowAsync<ArgumentNullException>("notification is null");
            await task3.Should().ThrowAsync<ArgumentNullException>("logger is null");
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="SyncTeamsActivity"/> class.
        /// </summary>
        private SyncTeamsActivity GetSyncTamActivity()
        {
            return new SyncTeamsActivity(this.teamDataRepository.Object, this.sentNotificationDataRepository.Object, this.localier.Object, this.notificationDataRepository.Object, this.recipientsService.Object);
        }
    }
}
