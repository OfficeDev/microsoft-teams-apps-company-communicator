// <copyright file="TeamsConversationOrchestratorTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Orchestrators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Utilities;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// TeamsConversationOrchestrator test class.
    /// </summary>
    public class TeamsConversationOrchestratorTest
    {
        private readonly Mock<IDurableOrchestrationContext> mockContext = new Mock<IDurableOrchestrationContext>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        /// <summary>
        /// Gets all the pending recipients and creates conversation with each recipient.
        /// 1. Checks if teams conversation activity is called exactly as the count of recipients.
        /// 2. Checks if each recipients batch partition key is updated to notification id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task TeamsConversationRunOrchestratorTest()
        {
            // Arrange
            string batchPartitionKey = "notificationId:1";
            IEnumerable<SentNotificationDataEntity> recipients = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity() { ConversationId = "conversationId1", PartitionKey = batchPartitionKey },

                new SentNotificationDataEntity() { ConversationId = "conversationId2", PartitionKey = batchPartitionKey },
            };

            var notificationId = PartitionKeyUtility.GetNotificationIdFromBatchPartitionKey(batchPartitionKey);
            this.mockContext
                .Setup(x => x.GetInput<string>())
                .Returns(batchPartitionKey);
            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<string>()))
                .ReturnsAsync(recipients);
            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await TeamsConversationOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.Is<string>(x => x.Equals(FunctionNames.GetPendingRecipientsActivity)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Once);
            this.mockContext
                .Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationActivity)), It.IsAny<RetryOptions>(), It.Is<(string notificationId, string batchPartitionKey, SentNotificationDataEntity recipients)>(x => x.recipients.PartitionKey.Equals(notificationId))), Times.Exactly(recipients.Count()));
        }

        /// <summary>
        /// Gets all the pending recipients and creates conversation with each recipient.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task TeamsConversationOrchestrator_NoRecipients_ShouldNotInvokeTeamsConversationActivity()
        {
            // Arrange
            string notificationId = "notificationId:1";
            IEnumerable<SentNotificationDataEntity> notification = new List<SentNotificationDataEntity>();

            this.mockContext
                .Setup(x => x.GetInput<string>())
                .Returns(notificationId);
            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<string>()))
                .ReturnsAsync(notification);
            this.mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await TeamsConversationOrchestrator.RunOrchestrator(this.mockContext.Object, this.mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.Is<string>(x => x.Equals(FunctionNames.GetPendingRecipientsActivity)), It.IsAny<RetryOptions>(), It.IsAny<string>()), Times.Once);
            this.mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.TeamsConversationActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Never);
        }
    }
}