// <copyright file="TeamsConversationOrchestratorTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Orchestrators
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using System.Linq;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Xunit;
    using FluentAssertions;


    /// <summary>
    /// TeamsConversationOrchestrator test class.
    /// </summary>
    public class TeamsConversationOrchestratorTest
    {
        private readonly Mock<IDurableOrchestrationContext> mockContext = new Mock<IDurableOrchestrationContext>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        /// <summary>
        /// Gets all the pending recipients and ceates conversation with each recipient.
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task TeamsConversationRunOrchestratorTest()
        {
            // Arrange
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "notificationId"
            };
            IEnumerable<SentNotificationDataEntity> notification = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity(){ ConversationId = "conversationId1"},
                new SentNotificationDataEntity(){ ConversationId = "conversationId2"}
            };

            mockContext
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);
            mockContext
                .Setup(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()))
                .ReturnsAsync(notification);
            mockContext
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await TeamsConversationOrchestrator.RunOrchestrator(mockContext.Object, mockLogger.Object);
            
            // Assert
            await task.Should().NotThrowAsync<Exception>();
            mockContext.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x=>x.Equals(FunctionNames.TeamsConversationActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Exactly(notification.Count()));
        }

    }
}
