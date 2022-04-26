﻿// <copyright file="NotificationServiceTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Test
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services;
    using Moq;
    using Xunit;

    /// <summary>
    /// NotificationService test class.
    /// </summary>
    public class NotificationServiceTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataReposioty = new Mock<INotificationDataRepository>();
        private readonly Mock<IGlobalSendingNotificationDataRepository> globalSendingNotificationDataRepository = new Mock<IGlobalSendingNotificationDataRepository>();
        private readonly SendQueueMessageContent sendQueueMessageContent = new SendQueueMessageContent()
        {
            RecipientData = new RecipientData()
            {
                RecipientId = "RecipientId1",
            },
            NotificationId = "notification1",
        };

        private readonly int sendRetryDelayNumberOfSeconds = 75;
        private readonly string notificationId = "notificationId";
        private readonly string recipientId = "RecipientId1";
        private readonly int totalNumberOfSendThrottles = 100;

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void NotificationServiceConstructorTest()
        {
            // Arrange
            Action action1 = () => new NotificationService(null /*globalSendingNotificationDataRepository*/, this.sentNotificationDataRepository.Object, this.notificationDataReposioty.Object);
            Action action2 = () => new NotificationService(this.globalSendingNotificationDataRepository.Object, null /*sentNotificationDataRepository*/, this.notificationDataReposioty.Object);
            Action action3 = () => new NotificationService(this.globalSendingNotificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.notificationDataReposioty.Object);
            Action action4 = () => new NotificationService(this.globalSendingNotificationDataRepository.Object, this.sentNotificationDataRepository.Object, null /*notificationDataReposioty*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("globalSendingNotificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action3.Should().NotThrow();
        }

        /// <summary>
        /// Test to check send notification is Throttled.
        /// </summary>
        /// <returns>returns boolean flag representing notification sent.</returns>
        [Fact]
        public async Task SendNotificationThrottledTest()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            var globalSendingNofificationDataResponse = new GlobalSendingNotificationDataEntity()
            {
                SendRetryDelayTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
            };
            this.globalSendingNotificationDataRepository
                .Setup(x => x.GetGlobalSendingNotificationDataEntityAsync())
                .ReturnsAsync(globalSendingNofificationDataResponse);

            // Act
            var serviceResponse = await notificationService.IsSendNotificationThrottled();

            // Assert
            serviceResponse.Should().BeFalse();
        }

        /// <summary>
        /// Test to check notification is sent when sendRetry delay time is null.
        /// </summary>
        /// <returns>returns boolean flag representing notification sent.</returns>
        [Fact]
        public async Task SendNotificationThrottled_SendRetrydelayTime_Test()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            GlobalSendingNotificationDataEntity globalSendingNofificationDataResponse = new GlobalSendingNotificationDataEntity();
            this.globalSendingNotificationDataRepository
                .Setup(x => x.GetGlobalSendingNotificationDataEntityAsync())
                .ReturnsAsync(globalSendingNofificationDataResponse);

            // Act
            var serviceResponse = await notificationService.IsSendNotificationThrottled();

            // Assert
            serviceResponse.Should().BeFalse();
        }

        /// <summary>
        /// Test method to handle exception when Recipient id is not set.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task NotificationPendingRecipientIdNotFoundTest()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            SendQueueMessageContent sendQueueMessageContent = new SendQueueMessageContent()
            {
                RecipientData = new RecipientData(),
            };

            // Act
            Func<Task> task = async () => await notificationService.IsPendingNotification(sendQueueMessageContent);

            // Assert
            await task.Should().ThrowAsync<InvalidOperationException>().WithMessage("Recipient id is not set.");
        }

        /// <summary>
        /// Test to avoid sending duplicate messages.
        /// If status code set to initializationStatusCode: this means the notification has not been attempted to be sent to this recipient.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task NotificationWithInitializationStatusTest()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            SentNotificationDataEntity notificationData = new SentNotificationDataEntity()
            {
                StatusCode = SentNotificationDataEntity.InitializationStatusCode,
            };
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);

            // Act
            var serviceResponse = await notificationService.IsPendingNotification(this.sendQueueMessageContent);

            // Assert
            serviceResponse.Should().BeTrue();
        }

        /// <summary>
        /// Test to avoid sending duplicate messages.
        /// If status code set to FaultedAndRetryingStatusCode: this means the Azure Function previously attempted to send the notification
        ///  to this recipient but failed and should be retried.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task NotificationFaultedAndRetryingStatusTest()
        {
            // Arrange
            var notificationService = this.GetNotificationService();

            SentNotificationDataEntity notificationData = new SentNotificationDataEntity()
            {
                StatusCode = SentNotificationDataEntity.FaultedAndRetryingStatusCode,
            };

            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);

            // Act
            var serviceResponse = await notificationService.IsPendingNotification(this.sendQueueMessageContent);

            // Assert
            serviceResponse.Should().BeTrue();
        }

        /// <summary>
        /// Test to check is notification is sent.
        /// </summary>
        /// <returns>returns boolean flag representing notification sent.</returns>
        [Fact]
        public async Task NotificationSentTest()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(SentNotificationDataEntity)));

            // Act
            var serviceResponse = await notificationService.IsPendingNotification(this.sendQueueMessageContent);

            // Assert
            serviceResponse.Should().BeFalse();
        }

        /// <summary>
        /// Test to set notification sent throttled.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SetSendNotificationThrottledTest()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            this.globalSendingNotificationDataRepository
                .Setup(x => x.SetGlobalSendingNotificationDataEntityAsync(It.IsAny<GlobalSendingNotificationDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await notificationService.SetSendNotificationThrottled(this.sendRetryDelayNumberOfSeconds);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.globalSendingNotificationDataRepository.Verify(x => x.SetGlobalSendingNotificationDataEntityAsync(It.IsAny<GlobalSendingNotificationDataEntity>()));
        }

        /// <summary>
        /// Test to update sent notification status as FaultedAndRetrying.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateSentNotification_Status_FaultedAndRetrying_Test()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            var notificationData = new SentNotificationDataEntity()
            {
                StatusCode = SentNotificationDataEntity.FaultedAndRetryingStatusCode,
                DeliveryStatus = SentNotificationDataEntity.Retrying,
            };
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            this.sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await notificationService.UpdateSentNotification(this.notificationId, this.recipientId, this.totalNumberOfSendThrottles, SentNotificationDataEntity.FaultedAndRetryingStatusCode, string.Empty, string.Empty);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<SentNotificationDataEntity>(x => x.StatusCode == notificationData.StatusCode)));
        }

        /// <summary>
        /// Test to update sent notification status created.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateSentNotification_Status_Created_Test()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            var notificationData = new SentNotificationDataEntity()
            {
                StatusCode = (int)HttpStatusCode.Created,
                DeliveryStatus = SentNotificationDataEntity.Succeeded,
            };
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            this.sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await notificationService.UpdateSentNotification(this.notificationId, this.recipientId, this.totalNumberOfSendThrottles, (int)HttpStatusCode.Created, string.Empty, string.Empty);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<SentNotificationDataEntity>(x => x.StatusCode == notificationData.StatusCode)));
        }

        /// <summary>
        /// Test to update sent notification status with too many requests.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateSentNotification_Status_TooManyRequest_Test()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            var notificationData = new SentNotificationDataEntity()
            {
                StatusCode = (int)HttpStatusCode.TooManyRequests,
                DeliveryStatus = SentNotificationDataEntity.Throttled,
            };
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            this.sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await notificationService.UpdateSentNotification(this.notificationId, this.recipientId, this.totalNumberOfSendThrottles, (int)HttpStatusCode.TooManyRequests, string.Empty, string.Empty);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<SentNotificationDataEntity>(x => x.StatusCode == notificationData.StatusCode)));
        }

        /// <summary>
        /// Test for update sent notification status as not found.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateSentNotification_Status_NotFound_Test()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            var notificationData = new SentNotificationDataEntity()
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                DeliveryStatus = SentNotificationDataEntity.RecipientNotFound,
            };
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            this.sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await notificationService.UpdateSentNotification(this.notificationId, this.recipientId, this.totalNumberOfSendThrottles, (int)HttpStatusCode.NotFound, string.Empty, string.Empty);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<SentNotificationDataEntity>(x => x.StatusCode == notificationData.StatusCode)));
        }

        /// <summary>
        /// Test for update sent notification status as failed.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateSentNotification_Status_Failed_Test()
        {
            // Arrange
            var notificationService = this.GetNotificationService();
            var notificationData = new SentNotificationDataEntity()
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                DeliveryStatus = SentNotificationDataEntity.Failed,
            };
            this.sentNotificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationData);
            this.sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await notificationService.UpdateSentNotification(this.notificationId, this.recipientId, this.totalNumberOfSendThrottles, 11, string.Empty, string.Empty);

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<SentNotificationDataEntity>(x => x.StatusCode == notificationData.StatusCode)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        private NotificationService GetNotificationService()
        {
            return new NotificationService(this.globalSendingNotificationDataRepository.Object, this.sentNotificationDataRepository.Object, this.notificationDataReposioty.Object);
        }
    }
}
