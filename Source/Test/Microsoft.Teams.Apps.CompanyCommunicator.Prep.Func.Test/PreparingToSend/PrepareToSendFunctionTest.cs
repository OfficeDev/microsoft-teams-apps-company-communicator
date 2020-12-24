// <copyright file="PrepareToSendFunctionTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test
{
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// PrepareToSendFunction test class.
    /// </summary>
    public class PrepareToSendFunctionTest
    {
        private readonly Mock<IDurableOrchestrationClient> starter = new Mock<IDurableOrchestrationClient>();
        private readonly Mock<ILogger> log = new Mock<ILogger>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();


        /// <summary>
        /// Constructor Test with null value.
        /// </summary> 
        [Fact]
        public void PrepareToSendFunctionConstructorNullValueTest()
        {
            // Arrange
            Action action1 = () => new PrepareToSendFunction(null /*notificationDataRepository*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
        }

        /// <summary>
        /// Constructor test.
        /// </summary> 
        [Fact]
        public void PrepareToSendFunctionConstructorSuccessTest()
        {
            // Arrange
            Action action1 = () => new PrepareToSendFunction(notificationDataRepository.Object);

            // Act and Assert.
            action1.Should().NotThrow();
        }
        /// <summary>
        /// SendNotificationData not found test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task PrepareToSendFunctionNotificationEntityNotFoundTest()
        {
            // Arrange
            var activityContext = this.GetPrepareToSendFunction();

            string myQueueItem = "{\"NotificationId\":\"notificationId\"}";
            PrepareToSendQueueMessageContent messageContent = JsonConvert.DeserializeObject<PrepareToSendQueueMessageContent>(myQueueItem);
            notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(NotificationDataEntity)));

            // Act
            Func<Task> task = async () => await activityContext.Run(myQueueItem, starter.Object, log.Object);

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x => x.GetAsync(It.Is<string>(x => x.Equals(NotificationDataTableNames.SentNotificationsPartition)), It.Is<string>(x => x.Equals(messageContent.NotificationId))), Times.Once());
            starter.Verify(x => x.StartNewAsync(It.IsAny<string>(), null /*instanceId*/), Times.Never());
        }

        /// <summary>
        /// PrepareToSendFunctionSuccess test
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task PrepareToSendFunctionSuccessTest()
        {
            // Arrange
            var activityContext = this.GetPrepareToSendFunction();

            string myQueueItem = "{\"NotificationId\":\"notificationId\"}";
            PrepareToSendQueueMessageContent messageContent = JsonConvert.DeserializeObject<PrepareToSendQueueMessageContent>(myQueueItem);
            NotificationDataEntity sentNotificationDataEntity = new NotificationDataEntity() { Id = "notificationId" };
            notificationDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sentNotificationDataEntity);
            starter.Setup(x => x.StartNewAsync(It.IsAny<string>(), It.IsAny<NotificationDataEntity>())).ReturnsAsync("instanceId");

            // Act
            Func<Task> task = async () => await activityContext.Run(myQueueItem, starter.Object, log.Object);

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x => x.GetAsync(It.Is<string>(x => x.Equals(NotificationDataTableNames.SentNotificationsPartition)), It.Is<string>(x => x.Equals(messageContent.NotificationId))), Times.Once());
            starter.Verify(x => x.StartNewAsync(It.Is<string>(x => x.Equals(FunctionNames.PrepareToSendOrchestrator)), It.Is<NotificationDataEntity>(x => x.Id == sentNotificationDataEntity.Id)), Times.Once());
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="GetPrepareToSendFunction"/> class.
        /// </summary>
        private PrepareToSendFunction GetPrepareToSendFunction()
        {
            return new PrepareToSendFunction(notificationDataRepository.Object);
        }
    }
}
