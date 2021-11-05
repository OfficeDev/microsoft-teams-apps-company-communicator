﻿// <copyright file="SendBatchMessagesActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// SendBatchMessagesActivity test class.
    /// </summary>
    public class SendBatchMessagesActivityTest
    {
        private readonly Mock<ISendQueue> sendQueue = new Mock<ISendQueue>();

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void SendBatchMessagesActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new SendBatchMessagesActivity(null /*sendQueue*/);
            Action action2 = () => new SendBatchMessagesActivity(this.sendQueue.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("sendQueue is null.");
            action2.Should().NotThrow();
        }

        /// <summary>
        /// Test for send batch messages activity success scenario for Recipient type "User data".
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SendBatchMessagesActivitySuccessTest()
        {
            // Arrange
            var activity = this.GetSendBatchMessagesActivity();
            List<SentNotificationDataEntity> batch = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity()
                {
                    RecipientType = SentNotificationDataEntity.UserRecipientType,
                    RecipientId = "recipientId",
                },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "123",
            };

            this.sendQueue
                .Setup(x => x.SendAsync(It.IsAny<IEnumerable<SendQueueMessageContent>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activity.RunAsync((notification.Id, batch));

            // Assert
            await task.Should().NotThrowAsync();
            this.sendQueue.Verify(x => x.SendAsync(It.Is<IEnumerable<SendQueueMessageContent>>(x => x.FirstOrDefault().RecipientData.RecipientType == RecipientDataType.User)));
        }

        /// <summary>
        /// Test for send batch messages activity success scenario for Reciepient type "Team data".
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SendBatchMessagesActivitySuccess_ForTeamRecipientTypeTest()
        {
            // Arrange
            var activity = this.GetSendBatchMessagesActivity();
            List<SentNotificationDataEntity> batch = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity()
                {
                    RecipientType = SentNotificationDataEntity.TeamRecipientType,
                    RecipientId = "recipientId",
                },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId",
            };

            this.sendQueue
                .Setup(x => x.SendAsync(It.IsAny<IEnumerable<SendQueueMessageContent>>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activity.RunAsync((notification.Id, batch));

            // Assert
            await task.Should().NotThrowAsync();
            this.sendQueue.Verify(x => x.SendAsync(It.Is<IEnumerable<SendQueueMessageContent>>(x => x.FirstOrDefault().RecipientData.RecipientType == RecipientDataType.Team)));
        }

        /// <summary>
        /// Failure test for Send batch messages as batch is null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SendBatchMessagesActivityFailureTest()
        {
            // Arrange
            var activity = this.GetSendBatchMessagesActivity();
            List<SentNotificationDataEntity> batch = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity()
                {
                    RecipientType = SentNotificationDataEntity.TeamRecipientType,
                    RecipientId = "recipientId",
                },
            };
            NotificationDataEntity notification = new NotificationDataEntity()
            {
                Id = "notificationId",
            };

            // Act
            Func<Task> task1 = async () => await activity.RunAsync((notification.Id, null /*batch*/));
            Func<Task> task2 = async () => await activity.RunAsync((null /*notification*/, batch));
            Func<Task> task3 = async () => await activity.RunAsync((null /*notification*/, null /*batch*/));

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>("batch is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("notification is null");
            await task3.Should().ThrowAsync<ArgumentNullException>("notification and batch are null");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendBatchMessagesActivity"/> class.
        /// </summary>
        private SendBatchMessagesActivity GetSendBatchMessagesActivity()
        {
            return new SendBatchMessagesActivity(this.sendQueue.Object);
        }
    }
}
