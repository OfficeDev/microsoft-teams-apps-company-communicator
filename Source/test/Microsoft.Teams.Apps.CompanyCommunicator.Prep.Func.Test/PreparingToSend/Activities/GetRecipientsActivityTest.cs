// <copyright file="GetRecipientsActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test
{
    using Xunit;
    using Moq;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using FluentAssertions;

    /// <summary>
    /// GetRecipientsActivity test class
    /// </summary>
    public class GetRecipientsActivityTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly string notificationId = "notificationId";
        IEnumerable<SentNotificationDataEntity> recipients = new List<SentNotificationDataEntity>()
        {
                new SentNotificationDataEntity() { RecipientId = "Test", ConversationId = string.Empty },
                new SentNotificationDataEntity() { RecipientId = "Test", ConversationId = "conversationId1" }
        };

        /// <summary>
        /// constuctor Tests
        /// </summary>
        [Fact]
        public void GetRecipientsActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new GetRecipientsActivity(null /*sentNotificationDataRepository*/);
            Action action2 = () => new GetRecipientsActivity(sentNotificationDataRepository.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action2.Should().NotThrow();
        }

        /// <summary>
        /// Get all the recipients from repository (Where some recipients have conversation id and some do not)
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetRecipientsSuccessTest()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            NotificationDataEntity notificationObj = new NotificationDataEntity()
            {
                Id = notificationId
            };
            sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null/*count*/))
           .ReturnsAsync(recipients);

            // Act
            var recipientsList = await activity.GetRecipientsAsync(notificationObj);

            // Assert
            recipientsList.Should().HaveCount(2);
            sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(notificationId)), null /*count*/));
        }

        /// <summary>
        /// Test for Get Recipients Activity failed when notification is null 
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetRecipientsFailureTest()
        {
            // Arrange
            var activity = this.GetRecipientsActivity();
            sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()))
           .ReturnsAsync(recipients);

            // Act
            Func<Task> task = async () => await activity.GetRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
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
                Id = notificationId
            };
            sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), null /*count*/))
                .ReturnsAsync(recipients);

            // Act
            var recipientsList = await activity.GetPendingRecipientsAsync(notificationObj);

            // Assert
            recipientsList.Should().HaveCount(1);
            sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.Is<string>(x => x.Equals(notificationId)), null /*count*/));
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
            sentNotificationDataRepository.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(recipients);

            // Act
            Func<Task> task = async () => await activity.GetPendingRecipientsAsync(null /*notification*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notification is null");
            sentNotificationDataRepository.Verify(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientsActivity"/> class.
        /// </summary>
        /// <returns>return the instance of GetRecipientsActivity</returns>
        private GetRecipientsActivity GetRecipientsActivity()
        {
            return new GetRecipientsActivity(sentNotificationDataRepository.Object);
        }
    }
}