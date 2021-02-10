// <copyright file="ExportFunctionTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Orchestrator;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Moq;
    using Xunit;

    /// <summary>
    /// ExportFunction test class.
    /// </summary>
    public class ExportFunctionTest
    {
        private readonly Mock<IDurableOrchestrationClient> starter = new Mock<IDurableOrchestrationClient>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<IExportDataRepository> exportDataRepository = new Mock<IExportDataRepository>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new ExportFunction(notificationDataRepository.Object, exportDataRepository.Object, localizer.Object);

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
            Action action1 = () => new ExportFunction(null /*notificationDataRepository*/, exportDataRepository.Object, localizer.Object);
            Action action2 = () => new ExportFunction(notificationDataRepository.Object, null /*exportDataRepository*/, localizer.Object);
            Action action3 = () => new ExportFunction(notificationDataRepository.Object, exportDataRepository.Object, null /*localizer*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("exportDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("localizer is null.");
        }

        /// <summary>
        /// Test case to check if activity handles null paramaters.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Theory]
        [MemberData(nameof(RunParameters))]
        public async Task RunActivity_NullParameters_ThrowsAgrumentNullException(string myQueueItem, Mock<IDurableOrchestrationClient> starter)
        {
            // Arrange
            var activityInstance = this.GetExportFunction();
            var mockStarter = starter?.Object;

            // Act
            Func<Task> task = async () => await activityInstance.Run(myQueueItem, mockStarter);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        public static IEnumerable<object[]> RunParameters
        {
            get
            {
                return new[]
                {
                    new object[] {  null, new Mock<IDurableOrchestrationClient>() },
                    new object[] {  "myQueueItem", null },
                };
            }
        }

        /// <summary>
        /// Test case to check if StartNewAsync method is called once to start ExportOrchestration.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Export_ForValidData_ShouldInvokeOnce()
        {
            // Arrange
            var activityInstance = GetExportFunction();
            string messageContent = "{\"NotificationId\":\"notificationId\",\"UserId\" : \"userId\"}";
            var notificationdata = new NotificationDataEntity();
            var exportDataEntity = new ExportDataEntity();
            var instanceId = "instanceId";

            notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationdata);
            exportDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(exportDataEntity);
            starter.Setup(x => x.StartNewAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(instanceId);

            // Act
            await activityInstance.Run(messageContent, starter.Object);

            // Assert
            starter.Verify(x => x.StartNewAsync(It.Is<string>(x => x.Equals("ExportOrchestrationAsync")), It.IsAny<ExportDataRequirement>()), Times.Once());
        }

        /// <summary>
        /// Test case to check if StartNewAsync method is never called to start ExportOrchestration.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Export_InvalidData_ShouldInvokeNever()
        {
            // Arrange
            var activityInstance = GetExportFunction();
            string messageContent = "{\"NotificationId\":\"notificationId\",\"UserId\" : \"userId\"}";
            var exportDataEntity = new ExportDataEntity();

            notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(default(NotificationDataEntity)));
            exportDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(exportDataEntity);
            starter.Setup(x => x.StartNewAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(It.IsAny<string>());

            // Act
            await activityInstance.Run(messageContent, starter.Object);

            // Assert
            starter.Verify(x => x.StartNewAsync(It.Is<string>(x => x.Equals("ExportOrchestrationAsync")), It.IsAny<ExportDataRequirement>()), Times.Never());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportFunction"/> class.
        /// </summary>
        /// <returns>return the instance of ExportFunction</returns>
        private ExportFunction GetExportFunction()
        {
            return new ExportFunction(notificationDataRepository.Object, exportDataRepository.Object, localizer.Object);
        }
    }
}


