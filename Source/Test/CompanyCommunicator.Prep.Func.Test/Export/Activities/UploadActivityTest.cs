// <copyright file="UploadActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams;
    using Moq;
    using Xunit;

    /// <summary>
    /// UploadActivity test class.
    /// </summary>
    public class UploadActivityTest
    {
        private readonly Mock<IStorageClientFactory> storageClientFactory = new Mock<IStorageClientFactory>();
        private readonly Mock<IDataStreamFacade> userDataStream = new Mock<IDataStreamFacade>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<BlobContainerClient> blobContainerClientMock = new Mock<BlobContainerClient>();
        private readonly string fileName = "fileName";

        /// <summary>
        /// Gets UploadParameters.
        /// </summary>
        public static IEnumerable<object[]> UploadParameters
        {
            get
            {
                return new[]
                {
                    new object[] { null, new Metadata(), "fileName" },
                    new object[] { new NotificationDataEntity(), null, "fileName" },
                    new object[] { new NotificationDataEntity(), new Metadata(), null },
                };
            }
        }

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new UploadActivity(this.storageClientFactory.Object, this.userDataStream.Object, this.localizer.Object);

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
            Action action1 = () => new UploadActivity(null /*storageClientFactory*/, this.userDataStream.Object, this.localizer.Object);
            Action action2 = () => new UploadActivity(this.storageClientFactory.Object, null/*userDataStream*/, this.localizer.Object);
            Action action3 = () => new UploadActivity(this.storageClientFactory.Object, this.userDataStream.Object, null/*localizer*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("storageClientFactory is null.");
            action2.Should().Throw<ArgumentNullException>("userDataStream is null.");
            action3.Should().Throw<ArgumentNullException>("localizer is null.");
        }

        /// <summary>
        /// Test case to check if method handles null paramaters.
        /// </summary>
        /// <param name="notificationDataEntity">the notification data entity.</param>
        /// <param name="metaData">metaData.</param>
        /// <param name="fileName">filename.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Theory]
        [MemberData(nameof(UploadParameters))]
        public async Task UploadActivity_NullParameters_ThrowsAgrumentNullException(
            NotificationDataEntity notificationDataEntity, Metadata metaData, string fileName)
        {
            // Arrange
            var activityInstance = this.GetUploadActivity();

            // Act
            Func<Task> task = async () => await activityInstance.UploadActivityAsync((notificationDataEntity, metaData, fileName));

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Test case to check if GetTeamDataStreamAsync shoud be invoked once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task NotificationWithTeams_GetTeamDataStreamAsync_ShouldInvodeOnce()
        {
            // Arrange
            var activityInstance = this.GetUploadActivity();
            var exportDataEntity = this.GetExportData();
            var userDataEntity = this.GetUserData();
            var notificationData = new NotificationDataEntity() { Teams = new List<string> { "team1" } };
            var metaData = new Metadata();
            var teamData = new List<TeamData>() { new TeamData() { Id = "id" } };
            var teamDatalist = new List<List<TeamData>>() { teamData };

            var mock = GetBlobContainerClientMock();
            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(Constants.BlobContainerName)).Returns(mock.Object);
            string metaDataFile = "FileName_Metadata";
            var metaDataFileName = new LocalizedString(metaDataFile, metaDataFile);
            this.localizer.Setup(_ => _[metaDataFile]).Returns(metaDataFileName);

            string fileNameMessage = "FileName_Message_Delivery";
            var fileNameMessageString = new LocalizedString(fileNameMessage, fileNameMessage);
            this.localizer.Setup(_ => _[fileNameMessageString]).Returns(metaDataFileName);
            this.userDataStream.Setup(x => x.GetTeamDataStreamAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(teamDatalist.ToAsyncEnumerable);

            // Act
            await activityInstance.UploadActivityAsync((notificationData, metaData, this.fileName));

            // Assert
            this.userDataStream.Verify(x => x.GetTeamDataStreamAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case to check if GetUserDataStreamAsync should be invoked once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task NotificationWithNoTeams_GetUserDataStreamAsync_ShouldInvodeOnce()
        {
            // Arrange
            var activityInstance = this.GetUploadActivity();
            var exportDataEntity = this.GetExportData();
            var userDataEntity = this.GetUserData();
            var notificationData = new NotificationDataEntity() { Teams = new List<string>() };
            var metaData = new Metadata();
            var userData = new List<UserData>() { new UserData() { Id = "id" } };
            var userDatalist = new List<List<UserData>>() { userData };

            var mock = GetBlobContainerClientMock();
            this.storageClientFactory.Setup(x => x.CreateBlobContainerClient(Constants.BlobContainerName)).Returns(mock.Object);
            string metaDataFile = "FileName_Metadata";
            var metaDataFileName = new LocalizedString(metaDataFile, metaDataFile);
            this.localizer.Setup(_ => _[metaDataFile]).Returns(metaDataFileName);

            string fileNameMessage = "FileName_Message_Delivery";
            var fileNameMessageString = new LocalizedString(fileNameMessage, fileNameMessage);
            this.localizer.Setup(_ => _[fileNameMessageString]).Returns(metaDataFileName);
            this.userDataStream.Setup(x => x.GetUserDataStreamAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(userDatalist.ToAsyncEnumerable);

            // Act
            await activityInstance.UploadActivityAsync((notificationData, metaData, this.fileName));

            // Assert
            this.userDataStream.Verify(x => x.GetUserDataStreamAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private static Mock<BlobContainerClient> GetBlobContainerClientMock()
        {
            var mock = new Mock<BlobContainerClient>();
            var blobClient = new Mock<BlobClient>();
            mock.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()));
            mock.Setup(x => x.SetAccessPolicyAsync(It.IsAny<PublicAccessType>(), It.IsAny<IEnumerable<BlobSignedIdentifier>>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()));
            mock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(blobClient.Object);
            blobClient.Setup(x => x.UploadAsync(It.IsAny<string>()));
            return mock;
        }

        private ExportDataEntity GetExportData()
        {
            return new ExportDataEntity() { FileName = "fileName", PartitionKey = "partitionKey" };
        }

        private UserDataEntity GetUserData()
        {
            return new UserDataEntity() { ServiceUrl = "serviceUrl", ConversationId = "conversationId" };
        }

        private UploadActivity GetUploadActivity()
        {
            return new UploadActivity(this.storageClientFactory.Object, this.userDataStream.Object, this.localizer.Object);
        }
    }
}