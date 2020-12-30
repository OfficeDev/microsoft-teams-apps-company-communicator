// <copyright file="GetMetadataActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;

    /// <summary>
    /// GetMetadataActivity test class.
    /// </summary>
    public class GetMetadataActivityTest
    {
        private readonly Mock<IUsersService> usersService = new Mock<IUsersService>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<IDurableOrchestrationContext> context = new Mock<IDurableOrchestrationContext>();
        private readonly Mock<ILogger> log = new Mock<ILogger>();

        /// <summary>
        /// Constructor test.
        /// </summary> 
        [Fact]
        public void GetMetadataActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new GetMetadataActivity(null /*userService*/, localizer.Object);
            Action action2 = () => new GetMetadataActivity(usersService.Object, null /**/);
            Action action3 = () => new GetMetadataActivity(usersService.Object, localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userService is null.");
            action2.Should().Throw<ArgumentNullException>("localizer is null.");
            action3.Should().NotThrow();
        }

        /// <summary>
        /// RunAsyncSuccess test.
        /// It creates and gets metadata.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RunAsyncSuccessTest()
        {
            // Arrange
            var getMetadataActivityInstance = GetMetadataActivity();
            var notificationDataEntity = new NotificationDataEntity();
            var exportDataEntity = new ExportDataEntity();
            var metaData = new Metadata() { MessageTitle = "messageTitle"};
            context.Setup(x => x.CallActivityWithRetryAsync<Metadata>(It.IsAny<string>(), It.IsAny<RetryOptions>(), (It.IsAny<Object>()))).ReturnsAsync(metaData);

            // Act
            var resultData = await getMetadataActivityInstance.RunAsync(context.Object, (notificationDataEntity, exportDataEntity), log.Object);

            // Assert
            resultData.Should().NotBeNull();
            context.Verify(x => x.CallActivityWithRetryAsync<Metadata>(It.IsAny<string>(), It.IsAny<RetryOptions>(), (It.IsAny<Object>())), Times.Once());
        }

        /// <summary>
        /// GetMetadataActivityAsync success test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetMetaDataActivityAsyncSuccessTest()
        {
            // Arrange
            var getMetadataActivityInstance = GetMetadataActivity();
            var notificationDataEntity = new NotificationDataEntity()
            {
                Title = "notificationTitle"
            };
            var exportDataEntity = new ExportDataEntity()
            {
                PartitionKey = "partitionKey"
            };

            User user = new User() { UserPrincipalName = "UserPrincipalName" };
            usersService.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync(user);

            // Act
            var metaData = await getMetadataActivityInstance.GetMetadataActivityAsync((notificationDataEntity, exportDataEntity));

            // Assert
            metaData.Should().NotBeNull();
            usersService.Verify(x => x.GetUserAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// GetMetadataActivity RunAsync argumentNullException test. 
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task GetMetadataActivity_RunAsyncNullArgumentTest()
        {
            // Arrange
            var activityInstance = this.GetMetadataActivity();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity();
            ExportDataEntity exportDataEntity = new ExportDataEntity();

            // Act
            Func<Task> task1 = async () => await activityInstance.RunAsync(null /*context*/, (notificationDataEntity, exportDataEntity), log.Object);
            Func<Task> task2 = async () => await activityInstance.RunAsync(context.Object, (null /*notificationDataEntity*/, exportDataEntity), log.Object);
            Func<Task> task3 = async () => await activityInstance.RunAsync(context.Object, (notificationDataEntity, null /*exportDataEntity*/), log.Object);

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>("context is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("notificationDataEntity is null");
            await task3.Should().ThrowAsync<ArgumentNullException>("exportDataEntity is null");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMetadataActivity"/> class.
        /// </summary>
        /// <returns>return the instance of GetMetadataActivity</returns>
        private GetMetadataActivity GetMetadataActivity()
        {
            return new GetMetadataActivity(usersService.Object, localizer.Object);
        }
    }
}

