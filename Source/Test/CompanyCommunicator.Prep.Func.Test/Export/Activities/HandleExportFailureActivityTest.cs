// <copyright file="HandleExportFailureActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Moq;
    using Xunit;

    /// <summary>
    /// HandleExportFailureActivity test class.
    /// </summary>
    public class HandleExportFailureActivityTest
    {
        private readonly Mock<IExportDataRepository> exportDataRepository = new Mock<IExportDataRepository>();
        private readonly Mock<IStorageClientFactory> storageClientFactory = new Mock<IStorageClientFactory>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<IOptions<BotOptions>> botOptions = new Mock<IOptions<BotOptions>>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<CCBotAdapter> botAdapter = new Mock<CCBotAdapter>(new Mock<ICertificateProvider>().Object, new Mock<BotFrameworkAuthentication>().Object);

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            this.botOptions.Setup(x => x.Value).Returns(new BotOptions() { AuthorAppId = "AuthorAppId" });
            Action action = () => new HandleExportFailureActivity(this.exportDataRepository.Object, this.storageClientFactory.Object, this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.localizer.Object);


            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Constructor test for null parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_NullParamters_ThrowsArgumentNullException()
        {
            // Arrange
            Action action1 = () => new HandleExportFailureActivity(null/*exportDataRepository*/, this.storageClientFactory.Object, this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.localizer.Object);
            Action action2 = () => new HandleExportFailureActivity(this.exportDataRepository.Object, null/*storageClientFactory*/, this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.localizer.Object);
            Action action3 = () => new HandleExportFailureActivity(this.exportDataRepository.Object, this.storageClientFactory.Object, null/*botOptions*/, this.botAdapter.Object, this.userDataRepository.Object, this.localizer.Object);
            Action action4 = () => new HandleExportFailureActivity(this.exportDataRepository.Object, this.storageClientFactory.Object, this.botOptions.Object, null/*botAdapter*/, this.userDataRepository.Object, this.localizer.Object);
            Action action5 = () => new HandleExportFailureActivity(this.exportDataRepository.Object, this.storageClientFactory.Object, this.botOptions.Object, this.botAdapter.Object, null/*userDataRepository*/, this.localizer.Object);
            Action action6 = () => new HandleExportFailureActivity(this.exportDataRepository.Object, this.storageClientFactory.Object, this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, null/*localizer*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("exportDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("storageClientFactory is null.");
            action3.Should().Throw<ArgumentNullException>("botOptions is null.");
            action4.Should().Throw<ArgumentNullException>("botAdapter is null.");
            action5.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action6.Should().Throw<ArgumentNullException>("localizer is null.");
        }

        /// <summary>
        /// Test case to check if method handles null paramaters.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task HandleFailure_NullParameters_ThrowsAgrumentNullException()
        {
            // Arrange
            var activityInstance = this.GetHandleExportFailureActivity();

            // Act
            Func<Task> task = async () => await activityInstance.HandleFailureActivityAsync(null/*exportDataEntity*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Test case to check if DeletIfExistsAsync shoud be never be invoked for invalid file.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task DeleteFile_nullParam_ShouldNeverInvokeDeleteFileFromStorage()
        {
            // Arrange
            var activityInstance = this.GetHandleExportFailureActivity();
            var exportDataEntity = new ExportDataEntity() { FileName = null, PartitionKey = "partitionKey" };
            var userDataEntity = new UserDataEntity() { ServiceUrl = "serviceUrl", ConversationId = "conversationId" };
            var blobContainerClientmock = GetBlobContainerClientMock();
            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(Constants.BlobContainerName)).Returns(blobContainerClientmock.Object);
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);

            // Act
            await activityInstance.HandleFailureActivityAsync(exportDataEntity);

            // Assert
            blobContainerClientmock.Verify(x => x.GetBlobClient(It.IsAny<string>()).DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Test case to check if DeletIfExistsAsync shoud be instanceated for valid file.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Delete_ValidFile_DeleteIfExistsAsyncShouldNeveOnce()
        {
            // Arrange
            var activityInstance = this.GetHandleExportFailureActivity();
            var exportDataEntity = new ExportDataEntity() { FileName = "fileName", PartitionKey = "partitionKey" };
            var userDataEntity = new UserDataEntity() { ServiceUrl = "serviceUrl", ConversationId = "conversationId" };
            var blobContainerClientmock = GetBlobContainerClientMock();
            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(Constants.BlobContainerName)).Returns(blobContainerClientmock.Object);
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);

            // Act
            await activityInstance.HandleFailureActivityAsync(exportDataEntity);

            // Assert
            blobContainerClientmock.Verify(x => x.GetBlobClient(It.IsAny<string>()).DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Test case to check callContinueConversationAsync should invoke Once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SendFailureMessage_CallContinueConversationAsync_ShouldInvokeOnce()
        {
            // Arrange
            var activityInstance = this.GetHandleExportFailureActivity();
            var exportDataEntity = new ExportDataEntity() { FileName = null, PartitionKey = "partitionKey" };
            var userDataEntity = new UserDataEntity() { ServiceUrl = "serviceUrl", ConversationId = "conversationId" };
            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(Constants.BlobContainerName)).Returns(default(BlobContainerClient));
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);
            string failureText = "ExportFailureText";
            var exportFailureString = new LocalizedString(failureText, failureText);
            this.localizer.Setup(_ => _[failureText]).Returns(exportFailureString);
            this.botAdapter.Setup(x => x.ContinueConversationAsync(It.IsAny<string>(), It.IsAny<ConversationReference>(), It.IsAny<BotCallbackHandler>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            await activityInstance.HandleFailureActivityAsync(exportDataEntity);

            // Assert
            this.botAdapter.Verify(x => x.ContinueConversationAsync(It.IsAny<string>(), It.IsAny<ConversationReference>(), It.IsAny<BotCallbackHandler>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Test case to check DeleteAsync invoked to delete entry for storage.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Delete_CallDeleteAsync_ShouldInvokeOnce()
        {
            // Arrange
            var activityInstance = this.GetHandleExportFailureActivity();
            var exportDataEntity = new ExportDataEntity() { FileName = null, PartitionKey = "partitionKey" };
            var userDataEntity = new UserDataEntity() { ServiceUrl = "serviceUrl", ConversationId = "conversationId" };
            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(Constants.BlobContainerName)).Returns(default(BlobContainerClient));
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userDataEntity);
            string failureText = "ExportFailureText";
            var exportFailureString = new LocalizedString(failureText, failureText);
            this.localizer.Setup(_ => _[failureText]).Returns(exportFailureString);
            this.botAdapter.Setup(x => x.ContinueConversationAsync(It.IsAny<string>(), It.IsAny<ConversationReference>(), It.IsAny<BotCallbackHandler>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            this.exportDataRepository.Setup(x => x.DeleteAsync(It.IsAny<ExportDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await activityInstance.HandleFailureActivityAsync(exportDataEntity);

            // Assert
            this.exportDataRepository.Verify(x => x.DeleteAsync(It.IsAny<ExportDataEntity>()), Times.Once);
        }

        private static Mock<BlobContainerClient> GetBlobContainerClientMock()
        {
            var mock = new Mock<BlobContainerClient>();
            mock.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()));
            mock.Setup(x => x.GetBlobClient(It.IsAny<string>()).DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()));
            return mock;
        }

        private HandleExportFailureActivity GetHandleExportFailureActivity()
        {
            this.botOptions.Setup(x => x.Value).Returns(new BotOptions() { AuthorAppId = "AuthorAppId" });
            return new HandleExportFailureActivity(this.exportDataRepository.Object, this.storageClientFactory.Object, this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.localizer.Object);
        }
    }
}