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
    using System.Collections.Generic;
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
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new UpdateExportDataActivity(exportDataRepository.Object);

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
        /// Test case to check if activity handles null paramaters.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Theory]
        [MemberData(nameof(RunParameters))]
        public async Task RunActivity_NullParameters_ThrowsAgrumentNullException(
            Mock<IDurableOrchestrationContext> context,
            ExportDataEntity exportDataEntity)
        {
            // Arrange
            var activityInstance = this.GetUpdateExportDataActivity();
            var mockContext = context?.Object;
            // Act
            Func<Task> task = async () => await activityInstance.RunAsync(mockContext, exportDataEntity, log.Object);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        public static IEnumerable<object[]> RunParameters
        {
            get
            {
                return new[]
                {
                    new object[] {  null, new ExportDataEntity() },
                    new object[] { new Mock<IDurableOrchestrationContext>(), null },
                };
            }
        }

        /// <summary>
        /// Test case to check CallActivityWithRetryAsync method is invoked once.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Update_CallExportDataService_ShouldInvokeOnce()
        {
            // Arrange
            var activityInstance = GetUpdateExportDataActivity();
            var exportDataEntity = GetExportDataEntity();

            context.Setup(x => x.CallActivityWithRetryAsync<Task>(It.IsAny<string>(), It.IsAny<RetryOptions>(), It.IsAny<ExportDataEntity>()));

            // Act
            Func<Task> task = async () => await activityInstance.RunAsync(context.Object, exportDataEntity, log.Object);

            // Assert
            await task.Should().NotThrowAsync();
            context.Verify(x => x.CallActivityWithRetryAsync<Task>(It.Is<string>(x => x.Equals(nameof(UpdateExportDataActivity.UpdateExportDataActivityAsync))), It.IsAny<RetryOptions>(), It.IsAny<ExportDataEntity>()), Times.Once);
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
        /// <returns></returns>
        [Fact]
        public async Task ExportData_CreateOrUpdateService_ShouldInvokeOnce()
        {
            // Arrange
            var activityInstance = GetUpdateExportDataActivity();
            var exportDataEntity = GetExportDataEntity();

            exportDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<ExportDataEntity>())).Returns(Task.CompletedTask);

            // Act
            await activityInstance.UpdateExportDataActivityAsync(exportDataEntity);

            // Assert
            exportDataRepository.Verify(x => x.CreateOrUpdateAsync(It.Is<ExportDataEntity>(x => x.FileConsentId == exportDataEntity.FileConsentId)), Times.Once);
        }

        private ExportDataEntity GetExportDataEntity()
        {
            return new ExportDataEntity()
            {
                PartitionKey = "partitionKey",
                SentDate = DateTime.Now,
                FileConsentId = "fileConsentId"
            };
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


