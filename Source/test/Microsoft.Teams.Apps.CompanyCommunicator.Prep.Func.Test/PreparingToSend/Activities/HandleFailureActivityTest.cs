// <copyright file="HandleFailureActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using System;
    using Xunit;
    using Moq;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using FluentAssertions;

    /// <summary>
    /// HandleFailureActivity test class
    /// </summary>
    public class HandleFailureActivityTest
    {
        private readonly Mock<Exception> excepion = new Mock<Exception>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();

        /// <summary>
        /// Consturctor tests.
        /// </summary>
        [Fact]
        public void HandleFailureActivityConstuctorTest()
        {
            // Arrange
            Action action1 = () => new HandleFailureActivity(null /*notificationDataRepository*/, localizer.Object);
            Action action2 = () => new HandleFailureActivity(notificationDataRepository.Object, null /*localizer*/);
            Action action3 = () => new HandleFailureActivity(null /*notificationDataRepository*/, null /*localizer*/);
            Action action4 = () => new HandleFailureActivity(notificationDataRepository.Object, localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("localizer is null.");
            action3.Should().Throw<ArgumentNullException>("notificationDataRepository and localizer are null.");
            action4.Should().NotThrow();
        }

        /// <summary>
        /// Success scenario of HandleFailureActivity.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task HandleFailureActivitySuccessTest()
        {
            // Arrange
            var activity = this.GetHandleFailureActivity();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "1"
            };
            notificationDataRepository
                .Setup(x => x.SaveExceptionInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activity.RunAsync((notificationDataEntity, excepion.Object));

            // Assert
            await task.Should().NotThrowAsync();
            notificationDataRepository.Verify(x => x.SaveExceptionInNotificationDataEntityAsync(It.Is<string>(x => x.Equals(notificationDataEntity.Id)), It.IsAny<string>()));

        }

        /// <summary>
        /// Failure scenario of HandleFailureActivity.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns
        [Fact]
        public async Task HandleFailureActivityNullArgumentTest()
        {
            // Arrange
            var activity = this.GetHandleFailureActivity();
            NotificationDataEntity notificationDataEntity = new NotificationDataEntity()
            {
                Id = "1"
            };
            notificationDataRepository
                .Setup(x => x.SaveExceptionInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task1 = async () => await activity.RunAsync((null /*notification*/, excepion.Object));
            Func<Task> task2 = async () => await activity.RunAsync((notificationDataEntity, null /*excepion.*/));
            Func<Task> task3 = async () => await activity.RunAsync((null /*notification*/, null /*excepion.*/));

            // Assert
            await task1.Should().ThrowAsync<ArgumentNullException>("notification is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("exception is null");
            await task3.Should().ThrowAsync<ArgumentNullException>("notification and excepion are null");
            notificationDataRepository.Verify(x => x.SaveExceptionInNotificationDataEntityAsync(It.Is<string>(x => x.Equals(notificationDataEntity.Id)), It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleFailureActivity"/> class.
        /// </summary>
        /// <returns>return the instance of HandleFailureActivity</returns>
        private HandleFailureActivity GetHandleFailureActivity()
        {
            return new HandleFailureActivity(notificationDataRepository.Object, localizer.Object);
        }
    }
}