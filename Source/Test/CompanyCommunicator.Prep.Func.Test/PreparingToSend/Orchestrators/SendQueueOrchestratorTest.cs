// <copyright file="SendQueueOrchestratorTest.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Orchestrators;
    using Moq;
    using Xunit;

    /// <summary>
    /// SendQueueOrchestrator test class.
    /// </summary>
    public class SendQueueOrchestratorTest
    {
        /// <summary>
        /// Reads the batch recipients. Sends messages to Send Queue in batches.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task RunOrchestratorTest()
        {
            // Arrange
            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            var mockLogger = new Mock<ILogger>();
            string batchPartitionKey = "1234:1";

            var recipients = new List<SentNotificationDataEntity>();
            for (int i = 0; i <= 100; i++)
            {
                recipients.Add(new SentNotificationDataEntity());
            }

            durableOrchestrationContextMock
                .Setup(x => x.GetInput<string>())
                .Returns(batchPartitionKey);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<string>()))
                .ReturnsAsync(recipients);

            var totalBatchesCount = recipients.AsBatches(SendQueue.MaxNumberOfMessagesInBatchRequest).ToList().Count;

            // Act
            Func<Task> task = async () => await SendQueueOrchestrator.RunOrchestrator(durableOrchestrationContextMock.Object, mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(It.Is<string>(x => x.Equals(FunctionNames.GetRecipientsActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendBatchMessagesActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Exactly(totalBatchesCount));
        }
    }
}