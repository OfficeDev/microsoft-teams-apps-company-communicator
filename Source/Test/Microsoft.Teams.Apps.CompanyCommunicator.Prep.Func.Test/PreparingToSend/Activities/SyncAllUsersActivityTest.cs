// <copyright file="SyncAllUsersActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
            //Arrange
            Action action1 = () => new SyncAllUsersActivity(null /*userDataRepository*/, sentNotificationDataRepository.Object, userService.Object, notificationDataRepository.Object, localier.Object);
            Action action2 = () => new SyncAllUsersActivity(userDataRepository.Object, null /*sentNotificationDataRepository*/, userService.Object, notificationDataRepository.Object, localier.Object);
            Action action3 = () => new SyncAllUsersActivity(userDataRepository.Object, sentNotificationDataRepository.Object, null /*userService*/, notificationDataRepository.Object, localier.Object);
            Action action4 = () => new SyncAllUsersActivity(userDataRepository.Object, sentNotificationDataRepository.Object, userService.Object, null /*notificationDataRepository*/, localier.Object);
            Action action5 = () => new SyncAllUsersActivity(userDataRepository.Object, sentNotificationDataRepository.Object, userService.Object, notificationDataRepository.Object, null /*localier*/);
            Action action6 = () => new SyncAllUsersActivity(userDataRepository.Object, sentNotificationDataRepository.Object, userService.Object, notificationDataRepository.Object, localier.Object);

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
        /// <returns></returns>
        [Fact]
        public async Task SyncAllUsersActivitySuccessTest()
        {
            // Arrange
            var activityContext = this.GetSyncAllUsersActivity();
            string deltaLink = "deltaLink";
            IEnumerable<UserDataEntity> useDataResponse = new List<UserDataEntity>()
            {
               new UserDataEntity() { Name = string.Empty }
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId1"
            };
            (IEnumerable<User>, string) tuple = (new List<User>() { new User() { Id = "100" } }, deltaLink);
            userDataRepository
                .Setup(x => x.GetDeltaLinkAsync())
                .ReturnsAsync(deltaLink);
            userService
                .Setup(x => x.GetAllUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(tuple);
            
            userDataRepository
                .Setup(x => x.SetDeltaLinkAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            userDataRepository
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), null))
                .ReturnsAsync(useDataResponse);
            userService
                .Setup(x => x.HasTeamsLicenseAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            //store user data
            userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);
            sentNotificationDataRepository.Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));

            // Act
            Func<Task> task = async () => await activityContext.RunAsync(notification);

            // Assert
            await task.Should().NotThrowAsync();
            userDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x=>x.RowKey == tuple.Item1.FirstOrDefault().Id)));
            sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()));
        }

        /// <summary>
        /// ArgumentNullException test for notification null.
        /// </summary>
        /// <returns></returns>
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
            return new SyncAllUsersActivity(userDataRepository.Object, sentNotificationDataRepository.Object, userService.Object, notificationDataRepository.Object, localier.Object);
        }
    }
}
