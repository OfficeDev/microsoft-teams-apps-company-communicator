// <copyright file="DataStreamFacadeTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Streams
{
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// DataStreamFacade test class.
    /// </summary>
    public class DataStreamFacadeTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        private readonly Mock<IUsersService> usersService = new Mock<IUsersService>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly string notificationId = "notificationId";
        private readonly IEnumerable<List<SentNotificationDataEntity>> data = new List<List<SentNotificationDataEntity>>()
            {
                new List<SentNotificationDataEntity>()
                {
                    new SentNotificationDataEntity(){ ConversationId = "conversationId" , DeliveryStatus = "Succeeded"}
                }
            };


        /// <summary>
        /// Constructor test.
        /// </summary> 
        [Fact]
        public void DataStreamFacadeConstructorTest()
        {
            // Arrange
            Action action1 = () => new DataStreamFacade(null /*sentNotificationDataRepository*/, teamDataRepository.Object, usersService.Object, localizer.Object);
            Action action2 = () => new DataStreamFacade(sentNotificationDataRepository.Object, null /*teamDataRepository*/, usersService.Object, localizer.Object);
            Action action3 = () => new DataStreamFacade(sentNotificationDataRepository.Object, teamDataRepository.Object, null /*usersService*/, localizer.Object);
            Action action4 = () => new DataStreamFacade(sentNotificationDataRepository.Object, teamDataRepository.Object, usersService.Object, null /*localizer*/);
            Action action5 = () => new DataStreamFacade(sentNotificationDataRepository.Object, teamDataRepository.Object, usersService.Object, localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("usersService is null.");
            action4.Should().Throw<ArgumentNullException>("localizer is null.");
            action5.Should().NotThrow();
        }


        /// <summary>
        /// Success test for to get the users data streams.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetUserDataStreamAsyncSuccesTest()
        {
            // Arrange
            var activityInstance = GetDataStreamFacade();
            IEnumerable<User> useData = new List<User>()
            {
                new User(){Id = "userDataId"}
            };

            sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(notificationId, null))
                .Returns(data.ToAsyncEnumerable());

            usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(useData);

            // Act
            var userDataStream = activityInstance.GetUserDataStreamAsync(notificationId);

            Func<Task> task = async () => await userDataStream.ForEachAsync(x => x.ToList());

            // Assert
            await task.Should().NotThrowAsync();
            sentNotificationDataRepository.Verify(x => x.GetStreamsAsync(It.Is<string>(x => x.Equals(notificationId)), null), Times.Once);
            usersService.Verify(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Success test for to get the team data streams.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetTeamDataStreamAsyncSuccesTest()
        {
            // Arrange
            var activityInstance = GetDataStreamFacade();
            var teamData = new TeamDataEntity();
            sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(notificationId, null))
                .Returns(data.ToAsyncEnumerable());

            teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(teamData);

            // Act
            var userDataStream = activityInstance.GetTeamDataStreamAsync(notificationId);

            Func<Task> task = async () => await userDataStream.ForEachAsync(x => x.ToList());

            // Assert
            await task.Should().NotThrowAsync();
            sentNotificationDataRepository.Verify(x => x.GetStreamsAsync(It.Is<string>(x => x.Equals(notificationId)), null), Times.Once);
            teamDataRepository.Verify(x => x.GetAsync(It.Is<string>(x=>x.Equals(TeamDataTableNames.TeamDataPartition)),It.IsAny<string>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// GetTeamDataStreamAsync argumentNullException test. 
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task GetTeamDataStreamAsyncNullArgumentTest()
        {
            // Arrange
            var activityInstance = GetDataStreamFacade();

            // Act
            Func<Task> task = async () => await activityInstance.GetTeamDataStreamAsync(null /*notificationId*/).ForEachAsync(x => x.ToList());

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
        }

        /// <summary>
        /// GetUserDataStreamAsync argumentNullException test. 
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task GetUserDataStreamAsyncNullArgumentTest()
        {
            // Arrange
            var activityInstance = GetDataStreamFacade();

            // Act
            Func<Task> task = async () => await activityInstance.GetTeamDataStreamAsync(null /*notificationId*/).ForEachAsync(x => x.ToList());

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DataStreamFacade"/> class.
        /// </summary>
        /// <returns>return the instance of DataStreamFacade</returns>
        private DataStreamFacade GetDataStreamFacade()
        {
            return new DataStreamFacade(sentNotificationDataRepository.Object, teamDataRepository.Object, usersService.Object, localizer.Object);
        }
    }
}



