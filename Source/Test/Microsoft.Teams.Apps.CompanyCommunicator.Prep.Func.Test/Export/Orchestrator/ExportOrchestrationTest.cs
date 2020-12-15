// <copyright file="ExportOrchestrationTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Orchestrator
{
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Orchestrator;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;

    /// <summary>
    /// ExportOrchestration test class.
    /// </summary>
    public class ExportOrchestrationTest
    {
        private readonly Mock<IUploadActivity> uploadActivity = new Mock<IUploadActivity>();
        private readonly Mock<ISendFileCardActivity> sendFileCardActivity = new Mock<ISendFileCardActivity>();
        private readonly Mock<IGetMetadataActivity> getMetadataActivity = new Mock<IGetMetadataActivity>();
        private readonly Mock<IUpdateExportDataActivity> updateExportDataActivity = new Mock<IUpdateExportDataActivity>();
        private readonly Mock<IHandleExportFailureActivity> handleExportFailureActivity = new Mock<IHandleExportFailureActivity>();
        private readonly Mock<IDurableOrchestrationContext> context = new Mock<IDurableOrchestrationContext>();
        private readonly Mock<ILogger> log = new Mock<ILogger>();

        /// <summary>
        /// Constructor test.
        /// </summary> 
        [Fact]
        public void GetMetadataActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new ExportOrchestration(null /*uploadActivity*/, sendFileCardActivity.Object, getMetadataActivity.Object, updateExportDataActivity.Object, handleExportFailureActivity.Object);
            Action action2 = () => new ExportOrchestration(uploadActivity.Object, null/*sendFileCardActivity*/, getMetadataActivity.Object, updateExportDataActivity.Object, handleExportFailureActivity.Object);
            Action action3 = () => new ExportOrchestration(uploadActivity.Object, sendFileCardActivity.Object, null/*getMetadataActivity*/, updateExportDataActivity.Object, handleExportFailureActivity.Object);
            Action action4 = () => new ExportOrchestration(uploadActivity.Object, sendFileCardActivity.Object, getMetadataActivity.Object, null/*updateExportDataActivity*/, handleExportFailureActivity.Object);
            Action action5 = () => new ExportOrchestration(uploadActivity.Object, sendFileCardActivity.Object, getMetadataActivity.Object, updateExportDataActivity.Object, null/*handleExportFailureActivity*/);
            Action action6 = () => new ExportOrchestration(uploadActivity.Object, sendFileCardActivity.Object, getMetadataActivity.Object, updateExportDataActivity.Object, handleExportFailureActivity.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("uploadActivity is null.");
            action2.Should().Throw<ArgumentNullException>("sendFileCardActivity is null.");
            action3.Should().Throw<ArgumentNullException>("getMetadataActivity is null.");
            action4.Should().Throw<ArgumentNullException>("updateExportDataActivity is null.");
            action5.Should().Throw<ArgumentNullException>("handleExportFailureActivity is null.");
            action6.Should().NotThrow();
        }

        /// <summary>
        /// Test case to check if the Orchestration successfully start the export process.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExportOrchestrationAsyncSuccessTest()
        {
            // Arrange
            var orchestratorInstance = GetExportOrchestration();
            var notificationDataEntity = new NotificationDataEntity()
            {
                Id = "notificationId"
            };
            var exportDataEntity = new ExportDataEntity()
            {
                FileName = "fileName"
            };
            var consentId = "consentId";
            var data = new Metadata();

            ExportDataRequirement exportDataRequirement = new ExportDataRequirement(notificationDataEntity, exportDataEntity, "userId");

            context.Setup(x => x.GetInput<ExportDataRequirement>())
                .Returns(exportDataRequirement);
            updateExportDataActivity.
                Setup(x => x.RunAsync(context.Object, It.IsAny<ExportDataEntity>(), log.Object))
                .Returns(Task.CompletedTask);
            getMetadataActivity
                .Setup(x => x.RunAsync(context.Object, It.IsAny<(NotificationDataEntity, ExportDataEntity)>(), log.Object))
                .ReturnsAsync(data);
            uploadActivity
                .Setup(x => x.RunAsync(context.Object, It.IsAny<(NotificationDataEntity, Metadata, string)>(), log.Object))
                .Returns(Task.CompletedTask);
            sendFileCardActivity
                .Setup(x => x.RunAsync(context.Object, It.IsAny<(string, string, string)>(), log.Object))
                .ReturnsAsync(consentId);

            // Act
            Func<Task> task = async () => await orchestratorInstance.ExportOrchestrationAsync(context.Object, log.Object);

            // Assert
            await task.Should().NotThrowAsync();
            getMetadataActivity.Verify(x => x.RunAsync(context.Object, It.IsAny<(NotificationDataEntity, ExportDataEntity)>(), log.Object), Times.Once);
            uploadActivity.Verify(x => x.RunAsync(context.Object, It.IsAny<(NotificationDataEntity, Metadata, string)>(), log.Object), Times.Once);
            sendFileCardActivity.Verify(x => x.RunAsync(context.Object, It.IsAny<(string, string, string)>(), log.Object), Times.Once);
            updateExportDataActivity.Verify(x => x.RunAsync(context.Object, It.IsAny<ExportDataEntity>(), log.Object), Times.Exactly(2));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportOrchestration"/> class.
        /// </summary>
        /// <returns>return the instance of ExportOrchestration</returns>
        private ExportOrchestration GetExportOrchestration()
        {
            return new ExportOrchestration(uploadActivity.Object, sendFileCardActivity.Object, getMetadataActivity.Object, updateExportDataActivity.Object, handleExportFailureActivity.Object);
        }
    }
}