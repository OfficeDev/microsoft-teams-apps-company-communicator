// <copyright file="RecipientsActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// RecipientsActivity test class.
    /// </summary>
    public class RecipientsActivityTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<IRecipientsService> recipientsService = new Mock<IRecipientsService>();
        private readonly string notificationId = "notificationId";
        private readonly IEnumerable<SentNotificationDataEntity> recipients = new List<SentNotificationDataEntity>()
        {
                new SentNotificationDataEntity() { RecipientId = "Test", ConversationId = string.Empty },
                new SentNotificationDataEntity() { RecipientId = "Test", ConversationId = "conversationId1" },
        };

        /// <summary>
        /// constructor Tests.
        /// </summary>
        [Fact]
        public void GetRecipientsActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new RecipientsActivity(null /*sentNotificationDataRepository*/, this.recipientsService.Object);
            Action action2 = () => new RecipientsActivity(this.sentNotificationDataRepository.Object, null);
            Action action3 = () => new RecipientsActivity(this.sentNotificationDataRepository.Object, this.recipientsService.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("recipientService is null.");
            action3.Should().NotThrow();
        }

        /// <summary>
        /// Get all the recipients from repository (Where some recipients have conversation id and some do not).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetRecipients_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            var activity = this.RecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
           .ReturnsAsync(this.recipients);

            // Act
            var result = await activity.GetRecipientsAsync(this.notificationId);

            // Assert
            result.Should().HaveCount(2);
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(this.notificationId)), null), Times.Once);
        }

        /// <summary>
        /// Test for Get Recipients Activity failed when notification is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetRecipients_NullParameter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var activity = this.RecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
           .ReturnsAsync(this.recipients);

            // Act
            Func<Task> task = async () => await activity.GetRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(this.notificationId)), null), Times.Never);
        }

        /// <summary>
        /// Get the batch recipients, which do not have a conversation id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetPendingRecipients_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            var activity = this.RecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
                .ReturnsAsync(this.recipients);

            // Act
            var recipientsList = await activity.GetPendingRecipientsAsync(this.notificationId);

            // Assert
            recipientsList.Should().HaveCount(1);
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(this.notificationId)), null /*count*/), Times.Once);
        }

        /// <summary>
        /// Test for Get pending recipients failure as NotificationDataEntity is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetPendingRecipients_NullParameter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var activity = this.RecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(this.recipients);

            // Act
            Func<Task> task = async () => await activity.GetPendingRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        /// <summary>
        /// Batch all the recipients and return the recipients information.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task BatchRecipients_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            var recipientsInfo = new RecipientsInfo(this.notificationId);
            var activity = this.RecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
                .ReturnsAsync(this.recipients);
            this.recipientsService.Setup(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .ReturnsAsync(recipientsInfo);

            // Act
            var result = await activity.BatchRecipientsAsync(this.notificationId);

            // Assert
            Assert.IsType<RecipientsInfo>(result);
            Assert.Equal(result.NotificationId, this.notificationId);
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(this.notificationId)), null /*count*/), Times.Once);
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Once);
        }

        /// <summary>
        /// Test case to check BatchRecipients when notification id is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task BatchRecipients_NullParameter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var recipientsInfo = new RecipientsInfo(this.notificationId);
            var activity = this.RecipientsActivity();
            this.sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
                .ReturnsAsync(this.recipients);
            this.recipientsService.Setup(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .ReturnsAsync(recipientsInfo);

            // Act
            Func<Task> task = async () => await activity.BatchRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            this.sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(this.notificationId)), null /*count*/), Times.Never);
            this.recipientsService.Verify(x => x.BatchRecipients(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Never);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientsActivity"/> class.
        /// </summary>
        /// <returns>return the instance of RecipientsActivity.</returns>
        private RecipientsActivity RecipientsActivity()
        {
            return new RecipientsActivity(this.sentNotificationDataRepository.Object, this.recipientsService.Object);
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