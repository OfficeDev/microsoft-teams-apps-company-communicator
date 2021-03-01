// <copyright file="UpdateExportDataActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Moq;
    using Xunit;

    /// <summary>
    /// UpdateExportDataActivity test class.
    /// </summary>
    public class UpdateExportDataActivityTest
    {
        private readonly Mock<ILogger> log = new Mock<ILogger>();
        private readonly Mock<IExportDataRepository> exportDataRepository = new Mock<IExportDataRepository>();

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new UpdateExportDataActivity(this.exportDataRepository.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Constructor test for null parameter.
        /// </summary>
        [Fact]
        public void CreateInstance_NullParamter_ThrowsArgumentNullException()
        {
            // Arrange
            Action action1 = () => new UpdateExportDataActivity(null /* exportDataRepository */);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("exportDataRepository is null.");
        }

        /// <summary>
        /// Test case to check CallActivityWithRetryAsync method is invoked once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task RunActivity_NullParameters_ThrowsAgrumentNullException()
        {
            // Arrange
            var activityInstance = this.GetUpdateExportDataActivity();

            // Act
            Func<Task> task = async () => await activityInstance.UpdateExportDataActivityAsync(null);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Test case to check ArgumentNullException when exportDataEntity argument is null for UpdateExportDataActivityAsync method.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UpdateExportData_NullParameters_ThrowsAgrumentNullException()
        {
            // Arrange
            var activityInstance = this.GetUpdateExportDataActivity();

            // Act
            Func<Task> task = async () => await activityInstance.UpdateExportDataActivityAsync(null /*exportDataEntity*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("exportDataEntity is null");
        }

        /// <summary>
        /// Test case to check if CreateOrUpdateAsync method is invoked once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ExportData_CreateOrUpdateService_ShouldInvokeOnce()
        {
            // Arrange
            var activityInstance = this.GetUpdateExportDataActivity();
            var exportDataEntity = this.GetExportDataEntity();

            this.exportDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<ExportDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await activityInstance.UpdateExportDataActivityAsync(exportDataEntity);

            // Assert
            this.exportDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<ExportDataEntity>(x => x.FileConsentId == exportDataEntity.FileConsentId)), Times.Once);
        }

        private ExportDataEntity GetExportDataEntity()
        {
            return new ExportDataEntity()
            {
                PartitionKey = "partitionKey",
                SentDate = DateTime.Now,
                FileConsentId = "fileConsentId",
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateExportDataActivity"/> class.
        /// </summary>
        /// <returns>return the instance of UpdateExportDataActivity.</returns>
        private UpdateExportDataActivity GetUpdateExportDataActivity()
        {
            return new UpdateExportDataActivity(this.exportDataRepository.Object);
        }
    }
}