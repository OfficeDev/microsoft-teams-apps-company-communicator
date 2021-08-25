// <copyright file="GetRecipientsActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// GetRecipientsActivity test class.
    /// </summary>
    public class GetRecipientsActivityTest
    {
        private const int MaxResultSize = 100000;
        private const int UserCount = 1000;

        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly string notificationId = "notificationId";
        private readonly IEnumerable<SentNotificationDataEntity> recipients = new List<SentNotificationDataEntity>()
        {
                new SentNotificationDataEntity() { RecipientId = "Test", ConversationId = string.Empty },
                new SentNotificationDataEntity() { RecipientId = "Test", ConversationId = "conversationId1" },
        };

        /// <summary>
        /// constuctor Tests.
        /// </summary>
        [Fact]
        public void GetRecipientsActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new GetRecipientsActivity(null /*sentNotificationDataRepository*/);
            Action action2 = () => new GetRecipientsActivity(this.sentNotificationDataRepository.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action2.Should().NotThrow();
        }

        /// <summary>
        /// Get all the recipients from repository (Where some recipients have conversation id and some do not).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetRecipientsSuccessTest()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            NotificationDataEntity notificationObj = new NotificationDataEntity()
            {
                Id = this.notificationId,
            };
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((this.recipients, null));

            // Act
            var result = await activity.GetRecipientsAsync(notificationObj);

            // Assert
            result.Item1.Should().HaveCount(2);
            Assert.Null(result.Item2);
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<int>(), It.IsAny<TableContinuationToken>()));
        }

        /// <summary>
        /// Get max recipients from repository.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetReceipients_ResultCount_ShouldNotExceedMaxResult()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            NotificationDataEntity notificationObj = new NotificationDataEntity()
            {
                Id = this.notificationId,
            };
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((this.GetRecipients(UserCount), new TableContinuationToken()));

            // Act
            var result = await activity.GetRecipientsAsync(notificationObj);

            // Assert
            result.Item1.Should().HaveCount(MaxResultSize);
            Assert.NotNull(result.Item2);
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<int>(), It.IsAny<TableContinuationToken>()));
        }

        /// <summary>
        /// Test if empty list is returned if no data is present.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetReceipients_NullData_ShouldReturnEmpty()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            NotificationDataEntity notificationObj = new NotificationDataEntity()
            {
                Id = this.notificationId,
            };
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((It.IsAny<List<SentNotificationDataEntity>>(), It.IsAny<TableContinuationToken>()));

            // Act
            var result = await activity.GetRecipientsAsync(notificationObj);

            // Assert
            Assert.Empty(result.Item1);
            Assert.Null(result.Item2);
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<int>(), It.IsAny<TableContinuationToken>()));
        }

        /// <summary>
        /// Test for Get Recipients Activity failed when notification is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetRecipientsFailureTest()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((this.recipients, null));

            // Act
            Func<Task> task = async () => await activity.GetRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()), Times.Never());
        }

        /// <summary>
        /// Get max recipients from repository.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetReceipientsyToken_ResultCount_ShouldNotExceedMaxResult()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((this.GetRecipients(UserCount), new TableContinuationToken()));

            // Act
            var result = await activity.GetRecipientsByTokenAsync((this.notificationId, new TableContinuationToken()));

            // Assert
            result.Item1.Should().HaveCount(MaxResultSize);
            Assert.NotNull(result.Item2);
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<int>(), It.IsAny<TableContinuationToken>()));
        }

        /// <summary>
        /// Test if empty list is returned if no data is present.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetReceipientsByToken_NullData_ShouldReturnEmpty()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((It.IsAny<List<SentNotificationDataEntity>>(), It.IsAny<TableContinuationToken>()));

            // Act
            var result = await activity.GetRecipientsByTokenAsync((this.notificationId, new TableContinuationToken()));

            // Assert
            Assert.Empty(result.Item1);
            Assert.Null(result.Item2);
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.Is<string>(x => x.Equals(this.notificationId)), It.IsAny<int>(), It.IsAny<TableContinuationToken>()));
        }

        /// <summary>
        /// Test for Get Recipients Activity By Token failed when token is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetActivityByToken_NullToken_ShouldThrowNullException()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()))
           .ReturnsAsync((this.recipients, null));

            // Act
            Func<Task> task = async () => await activity.GetRecipientsByTokenAsync((this.notificationId, null /*token*/));

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
            this.sentNotificationDataRepository.Verify(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TableContinuationToken>()), Times.Never());
        }

        /// <summary>
        /// Get all the recipients, which do not have a conversation id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetPendingRecipientsSuccessTest()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            NotificationDataEntity notificationObj = new NotificationDataEntity()
            {
                Id = this.notificationId,
            };
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
                .ReturnsAsync(this.recipients);

            // Act
            var recipientsList = await activity.GetPendingRecipientsAsync(notificationObj);

            // Assert
            recipientsList.Should().HaveCount(1);
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(this.notificationId)), null /*count*/));
        }

        /// <summary>
        /// Test for Get pending recipients failure as NotificationDataEntity is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetPendingRecipientsFailureTest()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(this.recipients);

            // Act
            Func<Task> task = async () => await activity.GetPendingRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientsActivity"/> class.
        /// </summary>
        /// <returns>return the instance of GetRecipientsActivity.</returns>
        private GetRecipientsActivity GetRecipientsActivity()
        {
            return new GetRecipientsActivity(this.sentNotificationDataRepository.Object);
        }

        private List<SentNotificationDataEntity> GetRecipients(int count)
        {
            var entities = new List<SentNotificationDataEntity>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(new SentNotificationDataEntity { RecipientId = string.Format("test_{0}", i) });
            }

            return entities;
        }
    }
}