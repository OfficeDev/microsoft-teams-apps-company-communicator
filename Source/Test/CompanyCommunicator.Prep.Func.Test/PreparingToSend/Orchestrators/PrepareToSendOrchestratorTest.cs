// <copyright file="PrepareToSendOrchestratorTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Orchestrators
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// Prepare to Send orchestratorTest.
    /// </summary>
    public class PrepareToSendOrchestratorTest
    {
        private readonly Mock<IDurableOrchestrationContext> mockContext = new Mock<IDurableOrchestrationContext>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        /// <summary>
        /// Prepare to send orchestrator success Test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestratorSuccessTest()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "123",
            };
            var recipientsInfo = new RecipientsInfo(notificationDataEntity.Id)
            {
                HasRecipientsPendingInstallation = true,
            };
            recipientsInfo.BatchKeys.Add("batchKey");

            this.mockContext
                .Setup(x => x.IsReplaying)
                .Returns(false);
            this.mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .ReturnsAsync(recipientsInfo);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Exactly(2));
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.HandleFailureActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Exactly(recipientsInfo.BatchKeys.Count));
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Exactly(recipientsInfo.BatchKeys.Count));
        }

        /// <summary>
        /// Test case to check that when there are no recipients having pending installation, teams conversation orchestrator should not be invoked.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestration_NoRecipientsPendingInstallation_ShouldNotInvokeTeamsConversationOrchestrator()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "123",
            };
            var recipientsInfo = new RecipientsInfo(notificationDataEntity.Id)
            {
                HasRecipientsPendingInstallation = false,
            };
            recipientsInfo.BatchKeys.Add("batchKey");

            this.mockContext
                .Setup(x => x.IsReplaying)
                .Returns(false);
            this.mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .ReturnsAsync(recipientsInfo);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Exactly(1));
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Exactly(recipientsInfo.BatchKeys.Count));
        }

        /// <summary>
        /// Test case to check that when there is null recipient info then invoke handle failure activity.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestration_NullRecipientInfo_ShouldInvokeHandleFailureActivity()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "123",
            };
            RecipientsInfo recipientsInfo = default;

            this.mockContext
                .Setup(x => x.IsReplaying)
                .Returns(false);
            this.mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .ReturnsAsync(recipientsInfo);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.HandleFailureActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        /// Test case to check that when there is an exception thrown on invoke of Sub Orchestrator then handle failure activity should be invoked.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestration_ExceptionThrownFromInvokedSubOrchestrator_ShouldInvokeHandleFailureActivity()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "123",
            };

            this.mockContext
                .Setup(x => x.IsReplaying)
                .Returns(false);
            this.mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Throws(new Exception());

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.HandleFailureActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        /// Test case to check that when there is an exception thrown on invoke of Sub Orchestrator then handle failure activity should be invoked.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestration_ExceptionThrownFromInvokedActivity_ShouldInvokeHandleFailureActivity()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "123",
            };
            RecipientsInfo recipientsInfo = default;
            this.mockContext
                .Setup(x => x.IsReplaying)
                .Returns(false);
            this.mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Throws(new Exception());
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .ReturnsAsync(recipientsInfo);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.HandleFailureActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        ///  PrepareToSendOrchestratorSuccess test with replaying flag true.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task PrepareToSendOrchestratorSuccessWithReplayingFlagTrueTest()
        {
            // Arrange
            Mock<NotificationDataEntity> mockNotificationDataEntity = new Mock<NotificationDataEntity>();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "notificationId",
            };
            var recipientsInfo = new RecipientsInfo(notificationDataEntity.Id)
            {
                HasRecipientsPendingInstallation = true,
            };
            recipientsInfo.BatchKeys.Add("batchKey");
            this.mockContext
                .Setup(x => x.IsReplaying)
                .Returns(true);

            this.mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);
            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .Returns(Task.CompletedTask);
            this.mockContext
                .Setup(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.IsAny<string>(), It.IsAny<RetryOptions>(), notificationDataEntity))
                .ReturnsAsync(recipientsInfo);

            // Act
            Func<Task> task = async () => await PrepareToSendOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.StoreMessageActivity)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Exactly(2));
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.HandleFailureActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync<RecipientsInfo>(It.Is<string>(x => x.Equals(FunctionNames.SyncRecipientsOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()), Times.Once());
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Exactly(recipientsInfo.BatchKeys.Count));
            this.mockContext.Verify(x => x.CallSubOrchestratorWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendQueueOrchestrator)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Exactly(recipientsInfo.BatchKeys.Count));
        }
    }
}
