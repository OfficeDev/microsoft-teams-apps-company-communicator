// <copyright file="SyncAllUsersActivityTest.cs" company="Microsoft">
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
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SyncAllUsersActivity test class.
    /// </summary>
    public class SyncAllUsersActivityTest
    {
        private readonly Mock<IUsersService> userService = new Mock<IUsersService>();
        private readonly Mock<IStringLocalizer<Strings>> localier = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void SyncAllUsersActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new SyncAllUsersActivity(null /*userDataRepository*/, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.localier.Object);
            Action action2 = () => new SyncAllUsersActivity(this.userDataRepository.Object, null /*sentNotificationDataRepository*/, this.userService.Object, this.notificationDataRepository.Object, this.localier.Object);
            Action action3 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, null /*userService*/, this.notificationDataRepository.Object, this.localier.Object);
            Action action4 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, null /*notificationDataRepository*/, this.localier.Object);
            Action action5 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, null /*localier*/);
            Action action6 = () => new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.localier.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("userService is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("localier is null.");
            action6.Should().NotThrow();
        }

        /// <summary>
        /// Success test for sync all user to repository.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SyncAllUsersActivitySuccessTest()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> useDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = string.Empty },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1",
            };
            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "100" } }, deltaLink);
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
                .ReturnsAsync(useDataResponse);
            this.userService
                .Setup(x => x.HasTeamsLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // store user data
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            this.sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(notification);

            // Assert
            await task.Should().NotThrowAsync();
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => x.RowKey == tuple.Item1.FirstOrDefault().Id)));
            this.sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));
        }

        /// <summary>
        /// ArgumentNullException test for notification null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ArgumentNullExceptionTest()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="SyncAllUsersActivity"/> class.
        /// </summary>
        private SyncAllUsersActivity GetSyncAllUsersActivity()
        {
            return new SyncAllUsersActivity(this.userDataRepository.Object, this.sentNotificationDataRepository.Object, this.userService.Object, this.notificationDataRepository.Object, this.localier.Object);
        }
    }
}
