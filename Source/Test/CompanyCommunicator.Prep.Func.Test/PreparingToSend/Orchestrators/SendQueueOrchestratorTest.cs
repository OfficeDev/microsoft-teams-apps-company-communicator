﻿// <copyright file="SendQueueOrchestratorTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Orchestrators
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SendQueueOrchestrator test class.
    /// </summary>
    public class SendQueueOrchestratorTest
    {
        /// <summary>
        /// Reads all the recipients , starts data aggregation. Sends messages to Send Queue in batches.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task RunOrchestratorTest()
        {
            // Arrange
            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            var mockLogger = new Mock<ILogger>();

            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "notificationId",
            };

            IEnumerable<SentNotificationDataEntity> sentNotificationDataEntitiesList = new List<SentNotificationDataEntity>();

            List<SentNotificationDataEntity> datalist = new List<SentNotificationDataEntity>();
            for (int i = 0; i <= 100; i++)
            {
                datalist.Add(new SentNotificationDataEntity());
            }

            sentNotificationDataEntitiesList = datalist;
            durableOrchestrationContextMock
                .Setup(x => x.GetInput<NotificationDataEntity>())
                .Returns(notificationDataEntity);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityWithRetryAsync(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityWithRetryAsync<(IEnumerable<SentNotificationDataEntity>, TableContinuationToken)>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<NotificationDataEntity>()))
                .ReturnsAsync((sentNotificationDataEntitiesList, new TableContinuationToken()));

            // Act
            Func<Task> task = async () => await SendQueueOrchestrator.RunOrchestrator(durableOrchestrationContextMock.Object, mockLogger.Object);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.UpdateNotificationStatusActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync<(IEnumerable<SentNotificationDataEntity>, TableContinuationToken)>(It.Is<string>(x => x.Equals(FunctionNames.GetRecipientsActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync<(IEnumerable<SentNotificationDataEntity>, TableContinuationToken)>(It.Is<string>(x => x.Equals(FunctionNames.GetRecipientsByTokenActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.DataAggregationTriggerActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.Once());
            durableOrchestrationContextMock.Verify(x => x.CallActivityWithRetryAsync(It.Is<string>(x => x.Equals(FunctionNames.SendBatchMessagesActivity)), It.IsAny<RetryOptions>(), It.IsAny<object>()), Times.AtLeast(1));
        }
    }
}