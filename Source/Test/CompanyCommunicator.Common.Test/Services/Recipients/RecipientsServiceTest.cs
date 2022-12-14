// <copyright file="RecipientsServiceTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Services.Recipients
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Utilities;
    using Moq;
    using Xunit;

    /// <summary>
    /// RecipientsService test.
    /// </summary>
    public class RecipientsServiceTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationRepository = new Mock<ISentNotificationDataRepository>();

        /// <summary>
        /// Test case to check if ArgumentNullException is thrown if parameters is null.
        /// </summary>
        [Fact]
        public void RecipientService_NullParameters_ShouldThrowArgumentNullException()
        {
            Action action1 = () => new RecipientsService(null);
            Action action2 = () => new RecipientsService(this.sentNotificationRepository.Object);

            action1.Should().Throw<ArgumentNullException>();
            action2.Should().NotThrow();
        }

        /// <summary>
        /// Test case to check if argument null exception is thrown when parameters are null.
        /// </summary>
        [Fact]
        public async void BatchRecipients_NullParameters_ShouldThrowArgumentNullException()
        {
            // Arrange
            var recipientService = this.GetRecipientsService();

            // Act
            Func<Task> task1 = async () => await recipientService.BatchRecipients(null /*recipients*/);

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Test case for success case.
        /// 1. Check if total recipient count is correct.
        /// 2. Check if HasRecipientsPendingInstallation is true, when there is no conversation id in the recipients.
        /// 3. Check if batch partition key was updated in the recipient partition key field.
        /// 4. Check if BatchInsertOrMerge call was invoked as many times as the batches count.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task BatchRecipients_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            var recipientService = this.GetRecipientsService();
            string notificationId = "notificationId";
            var recipients = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity() { PartitionKey = notificationId, ConversationId = "conversationId" },
                new SentNotificationDataEntity() { PartitionKey = notificationId, ConversationId = string.Empty },
                new SentNotificationDataEntity() { PartitionKey = notificationId, ConversationId = "conversationId2" },
            };

            this.sentNotificationRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            var recipientBatches = recipients.AsBatches(Constants.MaximumNumberOfRecipientsInBatch).ToList();

            // Act
            RecipientsInfo recipientsInfo = default;
            Func<Task> task = async () => recipientsInfo = await recipientService.BatchRecipients(recipients);

            // Assert
            await task.Should().NotThrowAsync();
            Assert.Equal(recipientsInfo.TotalRecipientCount, recipients.Count);
            Assert.True(recipientsInfo.HasRecipientsPendingInstallation);
            Assert.Equal(notificationId, PartitionKeyUtility.GetNotificationIdFromBatchPartitionKey(recipientsInfo.BatchKeys.First()));
            this.sentNotificationRepository.Verify(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()), Times.Exactly(recipientBatches.Count));
        }

        /// <summary>
        /// Test case to check if HasRecipientsPendingInstallation is false, when there all conversation id are filled.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task BatchRecipients_AllConverstaionId_HasPendingInstallationShouldBeFalse()
        {
            // Arrange
            var recipientService = this.GetRecipientsService();
            string notificationId = "notificationId";
            var recipients = new List<SentNotificationDataEntity>()
            {
                new SentNotificationDataEntity() { PartitionKey = notificationId, ConversationId = "conversationId" },
                new SentNotificationDataEntity() { PartitionKey = notificationId, ConversationId = "conversationId2" },
            };

            this.sentNotificationRepository
                .Setup(x => x.BatchInsertOrMergeAsync(It.IsAny<IEnumerable<SentNotificationDataEntity>>()))
                .Returns(Task.CompletedTask);

            var recipientBatches = recipients.AsBatches(Constants.MaximumNumberOfRecipientsInBatch).ToList();

            // Act
            RecipientsInfo recipientsInfo = default;
            Func<Task> task = async () => recipientsInfo = await recipientService.BatchRecipients(recipients);

            // Assert
            await task.Should().NotThrowAsync();
            Assert.False(recipientsInfo.HasRecipientsPendingInstallation);
        }

        private RecipientsService GetRecipientsService()
        {
            return new RecipientsService(this.sentNotificationRepository.Object);
        }
    }
}
