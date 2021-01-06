// <copyright file="UpdateExportDataActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// UpdateExportDataActivity test class.
    /// </summary>
    public class UpdateExportDataActivityTest
    {
        private readonly Mock<ILogger> log = new Mock<ILogger>();
        private readonly Mock<IExportDataRepository> exportDataRepository = new Mock<IExportDataRepository>();
        private readonly Mock<IDurableOrchestrationContext> context = new Mock<IDurableOrchestrationContext>();

        /// <summary>
        /// Constructor test.
        /// </summary> 
        [Fact]
        public void UpdateExportDataActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new UpdateExportDataActivity(null /* exportDataRepository */);
            Action action2 = () => new UpdateExportDataActivity(exportDataRepository.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("exportDataRepository is null.");
            action2.Should().NotThrow();
        }

        /// <summary>
        /// Test case to check if the update export data activity is invoked once.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RunAsyncSuccessTest()
        {
            // Arrange
            var activityInstance = GetUpdateExportDataActivity();
            ExportDataEntity exportDataEntity = new ExportDataEntity();

            context.Setup(x => x.CallActivityWithRetryAsync<Task>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<ExportDataEntity>()));

            // Act
            Func<Task> task = async () => await activityInstance.RunAsync(context.Object, exportDataEntity, log.Object);

            // Assert
            await task.Should().NotThrowAsync();
            context.Verify(x => x.CallActivityWithRetryAsync<Task>(It.Is<string>(x => x.Equals(nameof(UpdateExportDataActivity.UpdateExportDataActivityAsync))), It.IsAny<RetryOptions>(), It.IsAny<ExportDataEntity>()),Times.Once);
        }

        /// <summary> 
        /// Test case to check if the create or update export data entity is invoked once.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateExportDataActivityAsyncSuccessTest()
        {
            // Arrange
            var activityInstance = GetUpdateExportDataActivity();
            ExportDataEntity exportDataEntity = new ExportDataEntity() { FileConsentId = "fileConsentId" };

            exportDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<ExportDataEntity>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityInstance.UpdateExportDataActivityAsync(exportDataEntity);

            // Assert
            await task.Should().NotThrowAsync();
            exportDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<ExportDataEntity>(x=>x.FileConsentId == exportDataEntity.FileConsentId)), Times.Once);
        }

        /// <summary>
        /// UpdateExportDataActivity RunAsync argumentNullException test. 
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task UpdateExportDataActivity_RunAsyncNullArgumentTest()
        {
            // Arrange
            var activityInstance = this.GetUpdateExportDataActivity();
            ExportDataEntity exportDataEntity = new ExportDataEntity();

            // Act
            Func<Task> task1 = async () => await activityInstance.RunAsync(null/*context*/, It.IsAny<ExportDataEntity>(), log.Object);
            Func<Task> task2 = async () => await activityInstance.RunAsync(context.Object, null/*exportDataEntity*/, log.Object);

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>("context is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("exportDataEntity is null");
        }

        /// <summary>
        /// UpdateExportDataActivityAsync argumentNullException test. 
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task UpdateExportDataActivityAsyncNullArgumentTest()
        {
            // Arrange
            var activityInstance = this.GetUpdateExportDataActivity();
            ExportDataEntity exportDataEntity = new ExportDataEntity();

            // Act
            Func<Task> task = async () => await activityInstance.UpdateExportDataActivityAsync(null /*exportDataEntity*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("exportDataEntity is null");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateExportDataActivity"/> class.
        /// </summary>
        /// <returns>return the instance of UpdateExportDataActivity</returns>
        private UpdateExportDataActivity GetUpdateExportDataActivity()
        {
            return new UpdateExportDataActivity(exportDataRepository.Object);
        }
    }
}


