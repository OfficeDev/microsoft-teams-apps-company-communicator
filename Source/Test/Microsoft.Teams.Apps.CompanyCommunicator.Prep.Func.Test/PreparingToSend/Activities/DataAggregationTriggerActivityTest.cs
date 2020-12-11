// <copyright file="DataAggregationTriggerActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using Xunit;
    using Moq;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using System;
    using FluentAssertions;

    /// <summary>
    /// DataAggregationTriggerActivity test class
    /// </summary>
    public class DataAggregationTriggerActivityTest
    {
        private readonly Mock<IDataQueue> dataQueue = new Mock<IDataQueue>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<ILogger<NotificationDataRepository>> logger = new Mock<ILogger<NotificationDataRepository>>();
        private readonly int messageDelayInSeconds = 20;

        /// <summary>
        /// Consturctor tests.
        /// </summary>
        [Fact]
        public void DataAggregationTriggerActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new DataAggregationTriggerActivity(null /*notificationDataRepository*/, dataQueue.Object, Options.Create(new DataQueueMessageOptions()));
            Action action2 = () => new DataAggregationTriggerActivity(notificationDataRepository.Object, null /*dataQueue*/, Options.Create(new DataQueueMessageOptions()));
            Action action3 = () => new DataAggregationTriggerActivity(notificationDataRepository.Object, dataQueue.Object, null /*Ioptions<DataQueueMessageOptions>*/);
            Action action4 = () => new DataAggregationTriggerActivity(notificationDataRepository.Object, dataQueue.Object, Options.Create(new DataQueueMessageOptions() { MessageDelayInSeconds = messageDelayInSeconds }));

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("dataQueue is null.");
            action3.Should().Throw<ArgumentNullException>("options is null.");
            action4.Should().NotThrow();
        }
        
        /// <summary>
        /// Test to check update notificatin and send message to data queue.
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
                Id = notificationId
            };
            notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            notificationDataRepository
                .Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>()))
                .Returns(Task.CompletedTask);
            dataQueue
                .Setup(x => x.SendDelayedAsync(It.IsAny<DataQueueMessageContent>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCount), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals(notificationId))), Times.Once());
            notificationDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(x=>x.TotalMessageCount == recipientCount)));
            dataQueue.Verify(x => x.SendDelayedAsync(It.Is<DataQueueMessageContent>(x=>x.NotificationId == notificationId), It.Is<double>(x=>x.Equals(messageDelayInSeconds))));
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

            notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(NotificationDataEntity)));
            notificationDataRepository
                .Setup(x => x.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>()))
                .Returns(Task.CompletedTask);
            dataQueue
                .Setup(x => x.SendDelayedAsync(It.IsAny<DataQueueMessageContent>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCount), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals(notificationId))), Times.Once());
            notificationDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(x => x.TotalMessageCount == recipientCount)), Times.Never());
            dataQueue.Verify(x => x.SendDelayedAsync(It.Is<DataQueueMessageContent>(x => x.NotificationId == notificationId), It.Is<double>(x => x.Equals(messageDelayInSeconds))));
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
            Func<Task> task = async () => await dataAggregationTriggerActivity.RunAsync((null /*notificationId*/, recipientCount), logger.Object);

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
            Func<Task> task1 = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCountZero), logger.Object);
            Func<Task> task2 = async () => await dataAggregationTriggerActivity.RunAsync((notificationId, recipientCountNegative), logger.Object);
            
            // Assert
            await task1.Should().ThrowAsync<ArgumentOutOfRangeException>($"Recipient count should be > 0. Value: {recipientCountZero}");
            await task2.Should().ThrowAsync<ArgumentOutOfRangeException>($"Recipient count should be > 0. Value: {recipientCountNegative}");
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DataAggregationTriggerActivity"/> class.
        /// </summary>
        /// <returns>return the instance of DataAggregationTriggerActivity</returns>
        private DataAggregationTriggerActivity GetDataAggregationTriggerActivity()
        {
            return new DataAggregationTriggerActivity(notificationDataRepository.Object, dataQueue.Object, Options.Create(new DataQueueMessageOptions() { MessageDelayInSeconds = messageDelayInSeconds }));
        }
    }
}

