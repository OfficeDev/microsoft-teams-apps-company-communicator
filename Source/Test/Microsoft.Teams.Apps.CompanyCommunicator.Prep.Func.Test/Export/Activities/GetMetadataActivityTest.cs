// <copyright file="GetMetadataActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Moq;
    using Xunit;

    /// <summary>
    /// GetMetadataActivity test class.
    /// </summary>
    public class GetMetadataActivityTest
    {
        private readonly Mock<IUsersService> usersService = new Mock<IUsersService>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> log = new Mock<ILogger>();

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateActivity_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new GetMetadataActivity(this.usersService.Object, this.localizer.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Constructor test for null parameters.
        /// </summary>
        [Fact]
        public void CreateActivity_NullParamters_ThrowsArgumentNullException()
        {
            // Arrange
            Action action1 = () => new GetMetadataActivity(null /*userService*/, this.localizer.Object);
            Action action2 = () => new GetMetadataActivity(this.usersService.Object, null /*localizer*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userService is null.");
            action2.Should().Throw<ArgumentNullException>("localizer is null.");
        }

        /// <summary>
        /// Test case to check if activity handles null paramaters.
        /// </summary>
        /// <param name="context">context.</param>
        /// <param name="notificationDataEntity">notificationDataEntity.</param>
        /// <param name="exportDataEntity">exportDataEntity.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Theory]
        [MemberData(nameof(RunParameters))]
        public async Task RunActivity_NullParameters_ThrowsAgrumentNullException(
            Mock<IDurableOrchestrationContext> context,
            NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity)
        {
            // Arrange
            var activityInstance = this.GetMetadataActivity();
            var mockContext = context?.Object;

            // Act
            Func<Task> task = async () => await activityInstance.RunAsync(mockContext, (notificationDataEntity, exportDataEntity), log.Object);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Gets RunParameters.
        /// </summary>
        public static IEnumerable<object[]> RunParameters
        {
            get
            {
                return new[]
                {
                    new object[] { null, new NotificationDataEntity(), new ExportDataEntity() },
                    new object[] { new Mock<IDurableOrchestrationContext>(), null, new ExportDataEntity() },
                    new object[] { new Mock<IDurableOrchestrationContext>(), new NotificationDataEntity(), null },
                };
            }
        }

        /// <summary>
        /// Test case to check if activity returns Metadata type.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task RunActivity_CorrectType_ReturnsMetadataObject()
        {
            // Arrange
            var activityInstance = this.GetMetadataActivity();
            var context = new Mock<IDurableOrchestrationContext>();
            var notificationDataEntityMock = new Mock<NotificationDataEntity>();
            var exportDataEntityMock = new Mock<ExportDataEntity>();
            context.Setup(x => x.CallActivityWithRetryAsync<Metadata>(It.IsAny<string>(), It.IsAny<RetryOptions>(), (It.IsAny<Object>()))).ReturnsAsync(new Metadata());

            // Act
            var result = await activityInstance.RunAsync(context.Object, (notificationDataEntityMock.Object, exportDataEntityMock.Object), this.log.Object);

            // Assert
            Assert.Equal(typeof(Metadata), result.GetType());
        }

        /// <summary>
        /// Test case to check if get method handles null paramaters.
        /// </summary>
        /// <param name="notificationDataEntity">notificationDataEntity.</param>
        /// <param name="exportDataEntity">exportDataEntity.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Theory]
        [MemberData(nameof(GetParameters))]
        public async Task Get_NullParameters_ThrowsArgumentNullException(
            NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity)
        {
            // Arrange
            var activityInstance = this.GetMetadataActivity();

            // Act
            Func<Task> task = async () => await activityInstance.GetMetadataActivityAsync((notificationDataEntity, exportDataEntity));

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// GetParameters.
        /// </summary>
        public static IEnumerable<object[]> GetParameters
        {
            get
            {
                return new[]
                {
                    new object[] { null, new ExportDataEntity() },
                    new object[] { new NotificationDataEntity(), null },
                };
            }
        }

        /// <summary>
        /// Test case to check if GetUserAsync method is called once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CallUserService_ShouldInvokeOnce()
        {
            // Arrange
            var getMetadataActivityInstance = this.GetMetadataActivity();
            var notificationDataEntity = this.GetNotificationDataEntity();
            var exportDataEntity = this.GetExportDataEntity();
            var user = this.GetUser();
            this.usersService.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync(user);

            // Act
            var metaData = await getMetadataActivityInstance.GetMetadataActivityAsync((notificationDataEntity, exportDataEntity));

            // Assert
            this.usersService.Verify(x => x.GetUserAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case to check if service exception is thrown when received graph error which is not 403.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_GraphServiceError_ThrowsServiceException()
        {
            // Arrange
            var getMetadataActivityInstance = this.GetMetadataActivity();
            var notificationDataEntity = this.GetNotificationDataEntity();
            var exportDataEntity = this.GetExportDataEntity();
            var user = this.GetUser();
            var serviceException = new ServiceException(null, null, HttpStatusCode.Unauthorized);
            this.usersService.Setup(x => x.GetUserAsync(It.IsAny<string>())).ThrowsAsync(serviceException);

            // Act
            Func<Task> task = async () => await getMetadataActivityInstance.GetMetadataActivityAsync((notificationDataEntity, exportDataEntity));

            // Assert
            await task.Should().ThrowAsync<ServiceException>();
        }

        /// <summary>
        /// Test case to check that return object is not null and contains AdminConsentError.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_ForbiddenGraphPermission_ReturnsAdminConsentError()
        {
            // Arrange
            var getMetadataActivityInstance = this.GetMetadataActivity();
            var notificationDataEntity = this.GetNotificationDataEntity();
            var exportDataEntity = this.GetExportDataEntity();
            var user = this.GetUser();

            string key = "AdminConsentError";
            var localizedString = new LocalizedString(key, key);
            this.localizer.Setup(_ => _[key]).Returns(localizedString);
            var serviceException = new ServiceException(null, null, HttpStatusCode.Forbidden);
            this.usersService.Setup(x => x.GetUserAsync(It.IsAny<string>())).ThrowsAsync(serviceException);

            // Act
            var result = await getMetadataActivityInstance.GetMetadataActivityAsync((notificationDataEntity, exportDataEntity));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.ExportedBy, key);
        }

        /// <summary>
        /// Test case to check if object mapping is correct.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CorrectMapping_ReturnsMetadataObject()
        {
            // Arrange
            var getMetadataActivityInstance = this.GetMetadataActivity();
            var notificationDataEntity = this.GetNotificationDataEntity();
            var exportDataEntity = this.GetExportDataEntity();
            var user = this.GetUser();
            this.usersService.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync(user);

            // Act
            var metaData = await getMetadataActivityInstance.GetMetadataActivityAsync((notificationDataEntity, exportDataEntity));

            // Assert
            Assert.Equal(metaData.SentTimeStamp, notificationDataEntity.SentDate);
            Assert.Equal(metaData.ExportedBy, user.UserPrincipalName);
            Assert.Equal(metaData.MessageTitle, notificationDataEntity.Title);
            Assert.Equal(metaData.ExportTimeStamp, exportDataEntity.SentDate);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMetadataActivity"/> class.
        /// </summary>
        /// <returns>return the instance of GetMetadataActivity.</returns>
        private GetMetadataActivity GetMetadataActivity()
        {
            return new GetMetadataActivity(this.usersService.Object, this.localizer.Object);
        }

        private NotificationDataEntity GetNotificationDataEntity()
        {
            return new NotificationDataEntity()
            {
                Title = "notificationTitle",
                SentDate = DateTime.Now,
            };
        }

        private ExportDataEntity GetExportDataEntity()
        {
            return new ExportDataEntity()
            {
                PartitionKey = "partitionKey",
                SentDate = DateTime.Now,
            };
        }

        private User GetUser()
        {
            return new User()
            {
                UserPrincipalName = "UserPrincipalName",
            };
        }
    }
}