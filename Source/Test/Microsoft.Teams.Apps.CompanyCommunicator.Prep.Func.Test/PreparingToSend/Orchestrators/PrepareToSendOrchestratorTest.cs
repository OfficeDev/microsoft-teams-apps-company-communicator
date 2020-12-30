// <copyright file="PrepareToSendOrchestratorTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Orchestrators
{
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using FluentAssertions;

    /// <summary>
    /// Prepare to Send orchestratorTest.
    /// </summary>
    public class PrepareToSendOrchestratorTest
    {
        private readonly Mock<IDurableOrchestrationContext> mockContext = new Mock<IDurableOrchestrationContext>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        /// <summary>
        /// Prepare to send orchestrator success Test
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestratorSuccessTest()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "123"
            };
            mockContext
                .Setup(x => x.IsReplaying)
                .Returns(false);
            mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(mockContext.Object, mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x=>x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x=>x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
        }

        /// <summary>
        ///  PrepareToSendOrchestratorSuccess test with replaying flag true
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestratorSuccessWithReplayingFlagTrueTest()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "notificationId"
            };
            mockContext
                .Setup(x => x.IsReplaying)
                .Returns(true);

            mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);
            mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(mockContext.Object, mockLogger.Object);
            
            // Assert
            await task.Should().NotThrowAsync<Exception>();
            mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
        }
    }
}
