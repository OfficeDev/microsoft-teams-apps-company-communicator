// <copyright file="NotificationDataRepositoryTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Repositories.NotificationData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Blob;
    using Moq;
    using Xunit;

    /// <summary>
    /// Notification Data Repository unit tests.
    /// </summary>
    public class NotificationDataRepositoryTests
    {
        private Mock<ILogger<NotificationDataRepository>> logger = new Mock<ILogger<NotificationDataRepository>>();
        private Mock<IOptions<RepositoryOptions>> repositoryOptions = new Mock<IOptions<RepositoryOptions>>();
        private TableRowKeyGenerator rowKeyGenerator = new TableRowKeyGenerator();
        private Mock<IBlobStorageProvider> storageProvider = new Mock<IBlobStorageProvider>();

        /// <summary>
        /// Gets data for SaveExceptionInNotificationDataEntityAsync_SavesExceptionInfo and SaveWarningInNotificationDataEntityAsync_SavesWarningInfo.
        /// </summary>
        public static IEnumerable<object[]> SaveMessageTestCasesData
        {
            get
            {
                var testCases = new List<SaveMessageTestData>
                {
                    new SaveMessageTestData
                    {
                        InitialMessage = null,
                        ShouldUpdateMessage = true,
                        FailMessage = "Should update null message.",
                    },
                    new SaveMessageTestData
                    {
                        InitialMessage = "Short message",
                        ShouldUpdateMessage = true,
                        FailMessage = "Should update message not exceeding max length.",
                    },
                    new SaveMessageTestData
                    {
                        InitialMessage = new string('x', BaseRepository<NotificationDataEntity>.MaxMessageLengthToSave - 1),
                        ShouldUpdateMessage = false,
                        FailMessage = "Should not update message that will exceed max length.",
                    },
                    new SaveMessageTestData
                    {
                        InitialMessage = new string('x', BaseRepository<NotificationDataEntity>.MaxMessageLengthToSave),
                        ShouldUpdateMessage = false,
                        FailMessage = "Should not update message that is already at max length.",
                    },
                };
                return testCases.Select(c => new object[] { c });
            }
        }

        /// <summary>
        /// Check if NotificationData repository can be instantiated successfully.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            this.repositoryOptions.Setup(x => x.Value).Returns(new RepositoryOptions()
            {
                StorageAccountConnectionString = "UseDevelopmentStorage=true",
                EnsureTableExists = false,
            });
            Action action = () => new NotificationDataRepository(this.storageProvider.Object, this.logger.Object, this.repositoryOptions.Object, this.rowKeyGenerator);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Check that SaveExceptionInNotificationDataEntityAsync saves the exception info to table storage, up to a maximum length.
        /// </summary>
        /// <param name="testData">Test data.</param>
        /// <returns>Tracking task.</returns>
        [Theory]
        [MemberData(nameof(SaveMessageTestCasesData))]
        public async Task SaveExceptionInNotificationDataEntityAsync_SavesExceptionInfo(SaveMessageTestData testData)
        {
            const string testEntityId = "testEntityId";
            const string testMessage = "New error message.";

            // Arrange
            var mockRepository = this.CreateMockableNotificationDataRepository();
            var repository = mockRepository.Object;

            mockRepository.Setup(t => t.GetAsync(NotificationDataTableNames.SentNotificationsPartition, testEntityId))
                .Returns(Task.FromResult(new NotificationDataEntity
                {
                    Id = testEntityId,
                    ErrorMessage = testData.InitialMessage,
                }));
            mockRepository.Setup(t => t.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await repository.SaveExceptionInNotificationDataEntityAsync(testEntityId, testMessage);

            // Assert
            mockRepository.Verify(t => t.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(e =>
                e.Id == testEntityId &&
                e.Status == NotificationStatus.Failed.ToString())));

            if (testData.ShouldUpdateMessage)
            {
                mockRepository.Verify(
                    t => t.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(e =>
                        e.ErrorMessage != null && e.ErrorMessage.EndsWith(testMessage))),
                    testData.FailMessage);
            }
            else
            {
                mockRepository.Verify(
                    t => t.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(e =>
                        e.ErrorMessage == testData.InitialMessage)),
                    testData.FailMessage);
            }
        }

        /// <summary>
        /// Check that SaveWarningInNotificationDataEntityAsync saves the warning info to table storage, up to a maximum length.
        /// </summary>
        /// <param name="testData">Test data.</param>
        /// <returns>Tracking task.</returns>
        [Theory]
        [MemberData(nameof(SaveMessageTestCasesData))]
        public async Task SaveWarningInNotificationDataEntityAsync_SavesWarningInfo(SaveMessageTestData testData)
        {
            const string testEntityId = "testEntityId";
            const string testMessage = "New error message.";

            // Arrange
            var mockRepository = this.CreateMockableNotificationDataRepository();
            var repository = mockRepository.Object;

            mockRepository.Setup(t => t.GetAsync(NotificationDataTableNames.SentNotificationsPartition, testEntityId))
                .Returns(Task.FromResult(new NotificationDataEntity
                {
                    Id = testEntityId,
                    WarningMessage = testData.InitialMessage,
                }));
            mockRepository.Setup(t => t.CreateOrUpdateAsync(It.IsAny<NotificationDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await repository.SaveWarningInNotificationDataEntityAsync(testEntityId, testMessage);

            // Assert
            mockRepository.Verify(t => t.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(e =>
                e.Id == testEntityId)));

            if (testData.ShouldUpdateMessage)
            {
                mockRepository.Verify(
                    t => t.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(e =>
                        e.WarningMessage != null && e.WarningMessage.EndsWith(testMessage))),
                    testData.FailMessage);
            }
            else
            {
                mockRepository.Verify(
                    t => t.CreateOrUpdateAsync(It.Is<NotificationDataEntity>(e =>
                        e.WarningMessage == testData.InitialMessage)),
                    testData.FailMessage);
            }
        }

        private Mock<NotificationDataRepository> CreateMockableNotificationDataRepository()
        {
            this.repositoryOptions.Setup(x => x.Value).Returns(new RepositoryOptions()
            {
                StorageAccountConnectionString = "UseDevelopmentStorage=true",
                EnsureTableExists = false,
            });

            var mock = new Mock<NotificationDataRepository>(this.storageProvider.Object, this.logger.Object, this.repositoryOptions.Object, this.rowKeyGenerator);
            mock.CallBase = true;

            return mock;
        }

        /// <summary>
        /// Data for tests that check message saving to table.
        /// </summary>
        public class SaveMessageTestData
        {
            /// <summary>
            /// Gets or sets the initial message value.
            /// </summary>
            public string InitialMessage { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the message should be updated.
            /// </summary>
            public bool ShouldUpdateMessage { get; set; }

            /// <summary>
            /// Gets or sets the message to print if the test case fails.
            /// </summary>
            public string FailMessage { get; set; }
        }
    }
}
