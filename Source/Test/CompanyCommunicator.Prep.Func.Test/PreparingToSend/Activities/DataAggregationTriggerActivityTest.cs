// <copyright file="DataAggregationTriggerActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// DataAggregationTriggerActivity test class.
    /// </summary>
    public class DataAggregationTriggerActivityTest
    {
        private readonly Mock<IDataQueue> dataQueue = new Mock<IDataQueue>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<ILogger<NotificationDataRepository>> logger = new Mock<ILogger<NotificationDataRepository>>();
        private readonly int messageDelayInSeconds = 20;

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void DataAggregationTriggerActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new DataAggregationTriggerActivity(null /*notificationDataRepository*/, this.dataQueue.Object, Options.Create(new DataQueueMessageOptions()));
            Action action2 = () => new DataAggregationTriggerActivity(this.notificationDataRepository.Object, null /*dataQueue*/, Options.Create(new DataQueueMessageOptions()));
            Action action3 = () => new DataAggregationTriggerActivity(this.notificationDataRepository.Object, this.dataQueue.Object, null /*Ioptions<DataQueueMessageOptions>*/);
            Action action4 = () => new DataAggregationTriggerActivity(this.notificationDataRepository.Object, this.dataQueue.Object, Options.Create(new DataQueueMessageOptions() { MessageDelayInSeconds = this.messageDelayInSeconds }));

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("dataQueue is null.");
            action3.Should().Throw<ArgumentNullException>("options is null.");
            action4.Should().NotThrow();
        }

        /// <summary>
        /// Test to check update notification and send message to data queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task DataAggregationTriggerActivitySuccessTest()
        {
            // Arrange
            var dataAggregationTriggerActivity = this.GetDataAggregationTriggerActivity();
            var notificationId = "notificationId1";
            var recipientCount = 1;
            Mock<ILogger> logger = new Mock<ILogger>();
            NotificationDataEntity notificationData = new NotificationDataEntity()
            {
                Id = notificationId,
            };
            this.notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            this.notificationDataRepository
                .Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>()))
                .Returns(Task.CompletedTask);
            this.dataQueue
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            var messageDelayInTimeSpan = new TimeSpan(0, 0, this.messageDelayInSeconds);

            // Act
            Func<Task> task = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCount), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.notificationDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals(notificationId))), Times.Once());
            this.notificationDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(x => x.TotalMessageCount == recipientCount)));
            this.dataQueue.Verify(x => x.SendMessageAsync(It.Is<string>(x => x.Equals(notificationId)), It.Is<TimeSpan>(x => x.Equals(messageDelayInTimeSpan))));
        }

        /// <summary>
        /// Update notification was not done as notification data not found in repository for given notificationId.
        /// Send message to data queue is success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task DataAggregationTriggerActivityNotificationDataNotFound()
        {
            // Arrange
            var dataAggregationTriggerActivity = this.GetDataAggregationTriggerActivity();
            var notificationId = "notificationId1";
            var recipientCount = 1;
            Mock<ILogger> logger = new Mock<ILogger>();

            this.notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(NotificationDataEntity)));
            this.notificationDataRepository
                .Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>()))
                .Returns(Task.CompletedTask);
            this.dataQueue
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            var messageDelayInTimeSpan = new TimeSpan(0, 0, this.messageDelayInSeconds);

            // Act
            Func<Task> task = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCount), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.notificationDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals(notificationId))), Times.Once());
            this.notificationDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(x => x.TotalMessageCount == recipientCount)), Times.Never());
            this.dataQueue.Verify(x => x.SendMessageAsync(It.Is<string>(x => x.Equals(notificationId)), It.Is<TimeSpan>(x => x.Equals(messageDelayInTimeSpan))));
        }

        /// <summary>
        /// ArgumentNullException thrown for notificationId is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ArgumentNullException_For_NotificatoinNull_Test()
        {
            // Arrange
            var dataAggregationTriggerActivity = this.GetDataAggregationTriggerActivity();
            var recipientCount = 2;

            // Act
            Func<Task> task = async () => await dataAggregationTriggerActivity.RunAsync((null /*notificationId*/, recipientCount), this.logger.Object);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
        }

        /// <summary>
        /// ArgumentOutOfRangeException thrown for Recipient count is zero or negative.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task RecipientCountZeroOrNegative_ArgumentOutOfRangeException_Test()
        {
            // Arrange
            var dataAggregationTriggerActivity = this.GetDataAggregationTriggerActivity();
            var notificationId = "11";
            var recipientCountZero = 0;
            var recipientCountNegative = -1;

            // Act
            Func<Task> task1 = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCountZero), this.logger.Object);
            Func<Task> task2 = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCountNegative), this.logger.Object);

            // Assert
            await task1.Should().ThrowAsync<ArgumentOutOfRangeException>($"Recipient count should be > 0. Value: {recipientCountZero}");
            await task2.Should().ThrowAsync<ArgumentOutOfRangeException>($"Recipient count should be > 0. Value: {recipientCountNegative}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAggregationTriggerActivity"/> class.
        /// </summary>
        /// <returns>return the instance of DataAggregationTriggerActivity.</returns>
        private DataAggregationTriggerActivity GetDataAggregationTriggerActivity()
        {
            return new DataAggregationTriggerActivity(this.notificationDataRepository.Object, this.dataQueue.Object, Options.Create(new DataQueueMessageOptions() { MessageDelayInSeconds = this.messageDelayInSeconds }));
        }
    }
}