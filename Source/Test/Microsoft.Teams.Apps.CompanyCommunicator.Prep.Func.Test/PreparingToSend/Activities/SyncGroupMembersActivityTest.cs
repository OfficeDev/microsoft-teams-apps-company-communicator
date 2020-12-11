// <copyright file="SyncGroupMembersActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    extern alias BetaLib;
    using FluentAssertions;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Beta = BetaLib::Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;
    using System.Linq;


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

        /// <summary>
        /// Constructor tests.
        /// </summary> 
        [Fact]
        public void ConstructorArgumentNullException_Test()
        {
            // Arrange
            Action action1 = () => new SyncGroupMembersActivity(sentNotificationDataRepository.Object, notificationDataRepository.Object, groupMembersService.Object, null /*userDataRepository*/, localier.Object);
            Action action2 = () => new SyncGroupMembersActivity(sentNotificationDataRepository.Object, notificationDataRepository.Object, groupMembersService.Object, userDataRepository.Object, null /*localier*/);
            Action action3 = () => new SyncGroupMembersActivity(sentNotificationDataRepository.Object, notificationDataRepository.Object, null /*groupMembersService*/, userDataRepository.Object, localier.Object);
            Action action4 = () => new SyncGroupMembersActivity(sentNotificationDataRepository.Object, null /*notificationDataRepository*/, groupMembersService.Object, userDataRepository.Object, localier.Object);
            Action action5 = () => new SyncGroupMembersActivity(null /*sentNotificationDataRepository*/, notificationDataRepository.Object, groupMembersService.Object, userDataRepository.Object, localier.Object);
            Action action6 = () => new SyncGroupMembersActivity(sentNotificationDataRepository.Object, notificationDataRepository.Object, groupMembersService.Object, userDataRepository.Object, localier.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("localier is null.");
            action3.Should().Throw<ArgumentNullException>("groupMembersService is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action6.Should().NotThrow();
        }

        /// <summary>
        /// Success Test to Syncs group members to repository.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task SyncGroupMembersActivitySuccessTest()
        {
            // Arrange
            var groupId = "Group1";
            var notificationId = "notificaionId";
            var activityContext = this.GetSyncGroupMembersActivity();
            var users = new List<User>()
            {
                new User(){Id = "userId"}
            };
            groupMembersService
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string>()))
                .ReturnsAsync(users);
            userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(UserDataEntity)));
            sentNotificationDataRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((notificationId, groupId), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            sentNotificationDataRepository.Verify(x => x.BatchInsertOrMergeAsync(It.Is<IEnumerable<SentNotificationDataEntity>>(x=>x.FirstOrDefault().PartitionKey == notificationId)));
        }

        /// <summary>
        /// ArgumentNullException Test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task ArgumentNullExceptionTest()
        {
            // Arrange
            var groupId = "GroupId";
            var notificationId = "noticationId";
            var activityContext = this.GetSyncGroupMembersActivity();

            // Act
            Func<Task> task = async () => await activityContext.RunAsync((null /*notificationId*/, groupId), logger.Object);
            Func<Task> task1 = async () => await activityContext.RunAsync((notificationId, null /*groupId*/), logger.Object);
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
            return new SyncGroupMembersActivity(sentNotificationDataRepository.Object, notificationDataRepository.Object, groupMembersService.Object, userDataRepository.Object, localier.Object);
        }
    }
}
