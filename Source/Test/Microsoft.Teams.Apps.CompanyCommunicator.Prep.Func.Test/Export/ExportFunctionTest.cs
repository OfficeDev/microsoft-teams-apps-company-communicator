// <copyright file="ExportFunctionTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export
{
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

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
        /// Constructor test.
        /// </summary> 
        [Fact]
        public void ExportFunctionConstructorTest()
        {
            // Arrange
            Action action1 = () => new ExportFunction(null /*notificationDataRepository*/, exportDataRepository.Object, localizer.Object);
            Action action2 = () => new ExportFunction(notificationDataRepository.Object, null /*exportDataRepository*/, localizer.Object);
            Action action3 = () => new ExportFunction(notificationDataRepository.Object, exportDataRepository.Object, null /*localizer*/);
            Action action4 = () => new ExportFunction(notificationDataRepository.Object, exportDataRepository.Object, localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("exportDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("localizer is null.");
            action4.Should().NotThrow();
        }

        /// <summary>
        /// ExportFunction RunAsyncSuccess test
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExportFunctionRunSuccessTest()
        {
            // Arrange
            var activityInstance = GetExportFunction();
            string messageContent = "{\"NotificationId\":\"notificationId\",\"UserId\" : \"userId\"}";
            var notificationdata = new NotificationDataEntity();
            var exportDataEntity = new ExportDataEntity();

            notificationDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(notificationdata);
            exportDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(exportDataEntity);

            starter.Setup(x => x.StartNewAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(It.IsAny<string>);

            // Act
            Func<Task> task = async () => await activityInstance.Run(messageContent, starter.Object);

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals("notificationId"))), Times.Once());
            exportDataRepository.Verify(x => x.GetAsync(It.IsAny<string>(), It.Is<string>(x => x.Equals("notificationId"))), Times.Once());
        }


        /// <summary>
        /// ExportFunction argumentNullException test. 
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task ExportFunctionNullArgumentTest()
        {
            // Arrange
            var activityInstance = this.GetExportFunction();
            string messageContent = "{\"NotificationId\":\"notificationId\",\"UserId\" : \"userId\"}";

            // Act
            Func<Task> task1 = async () => await activityInstance.Run(null /*messageContent*/, starter.Object);
            Func<Task> task2 = async () => await activityInstance.Run(messageContent, null /*starter*/);
            Func<Task> task3 = async () => await activityInstance.Run(null /*messageContent*/, null/*starter*/);

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>("messageContent is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("starter is null");
            await task3.Should().ThrowAsync<ArgumentNullException>();
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


