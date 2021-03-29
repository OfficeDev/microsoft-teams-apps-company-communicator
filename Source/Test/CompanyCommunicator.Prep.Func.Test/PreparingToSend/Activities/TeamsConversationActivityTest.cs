// <copyright file="TeamsConversationActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;
    using Moq;
    using Xunit;

    /// <summary>
    /// TeamsConversationActivity test class.
    /// </summary>
    public class TeamsConversationActivityTest
    {
        private readonly Mock<IConversationService> conversationService = new Mock<IConversationService>();
        private readonly Mock<IAppManagerService> appManagerService = new Mock<IAppManagerService>();
        private readonly Mock<IChatsService> chatsService = new Mock<IChatsService>();
        private readonly Mock<IAppSettingsService> appSettingsService = new Mock<IAppSettingsService>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly int maxAttempts = 10;

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void TeamsConversationActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, null /*userDataRepository*/, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action2 = () => new TeamsConversationActivity(null /*conversationService*/, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action3 = () => new TeamsConversationActivity(this.conversationService.Object, null /*sentNotificationDataRepository*/, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action4 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, null /*notificationDataRepository*/, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action5 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, null /*appManagerService*/, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action6 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, null /*chatsService*/, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action7 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, null /*appSettingsService*/, Options.Create(new TeamsConversationOptions()), this.localizer.Object);
            Action action8 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), null /*localizer*/);
            Action action9 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, null /*options*/, this.localizer.Object);
            Action action10 = () => new TeamsConversationActivity(this.conversationService.Object, this.sentNotificationDataRepository.Object, this.userDataRepository.Object, this.notificationDataRepository.Object, this.appManagerService.Object, this.chatsService.Object, this.appSettingsService.Object, Options.Create(new TeamsConversationOptions()), this.localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("conversationService is null.");
            action3.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action4.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action5.Should().Throw<ArgumentNullException>("appManagerService is null.");
            action6.Should().Throw<ArgumentNullException>("chatsService is null.");
            action7.Should().Throw<ArgumentNullException>("appSettingsService is null.");
            action8.Should().Throw<ArgumentNullException>("options is null.");
            action9.Should().Throw<ArgumentNullException>("localizer is null.");
            action10.Should().NotThrow();
        }

        /// <summary>
        /// Test to check TeamRecipientType not null. No-op for Team recipient type.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversationAsyncTest_TeamRecipientType()
        {
            // Arrange
            var activityContext = this.GetTeamsConversationActivity();
            var notificationId = "notificationId";
            SentNotificationDataEntity reciepient = new SentNotificationDataEntity()
            {
                RecipientType = SentNotificationDataEntity.TeamRecipientType,
            };

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, reciepient), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
        }

        /// <summary>
        /// Success scenario to create conversation for users with teams user id.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversationAsync()
        {
            // Arrange
            string notificationId = "notificationId";
            string serviceUrl = "serviceUrlAppSettings";
            var activityContext = this.GetTeamsConversationActivity();
            SentNotificationDataEntity recipient = new SentNotificationDataEntity()
            {
                UserId = "userId",
                TenantId = "tenantId",
                ServiceUrl = "serviceUrl",
            };
            CreateConversationResponse response = new CreateConversationResponse()
            {
                Result = Result.Succeeded,
                ConversationId = "conversationid",
            };
            this.conversationService
                .Setup(x => x.CreateUserConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.logger.Object))
                .ReturnsAsync(response);
            this.appSettingsService
                .Setup(x => x.GetServiceUrlAsync())
                .Returns(Task.FromResult(serviceUrl));
            this.sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);
            this.userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, recipient), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.conversationService.Verify(x => x.CreateUserConversationAsync(
                It.Is<string>(x => recipient.UserId.Equals(x)),
                It.Is<string>(x => recipient.TenantId.Equals(x)),
                It.Is<string>(x => recipient.ServiceUrl.Equals(x)),
                It.IsAny<int>(),
                this.logger.Object));
            this.userDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => recipient.UserId.Equals(x.UserId))));
        }

        /// <summary>
        /// Conversation not created as Proactive app installation is disabled.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversationAsync_UserIdNullOrEmpty()
        {
            // Arrange
            var activityContext = this.GetTeamsConversationActivity(false/*proactivelyInstallUserApp*/);
            var notificationId = "notificationId";
            SentNotificationDataEntity recipient = new SentNotificationDataEntity()
            {
                UserId = string.Empty,
                RecipientId = "recipientId",
            };

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, recipient), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
        }

        /// <summary>
        /// Create Conversation check when Proactive app installation flag enabled. ConversationId is empty.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ProactiveAppInstallationEnabledTest()
        {
            // Arrange
            var activityContext = this.GetTeamsConversationActivity(true/*proactivelyInstallUserApp*/);
            var notificationId = "notificationId";
            var appId = "appId";
            var chatId = "chatId";
            string serviceUrl = "serviceUrl";
            SentNotificationDataEntity recipient = new SentNotificationDataEntity()
            {
                UserId = string.Empty,
                RecipientId = "recipientId",
            };

            this.appSettingsService
                .Setup(x => x.GetUserAppIdAsync())
                .Returns(Task.FromResult(appId));
            this.appManagerService
                .Setup(x => x.InstallAppForUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            this.chatsService
                .Setup(x => x.GetChatThreadIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(chatId));
            this.appSettingsService
                .Setup(x => x.GetServiceUrlAsync())
                .Returns(Task.FromResult(serviceUrl));

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, recipient), this.logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            this.appManagerService.Verify(x => x.InstallAppForUserAsync(
                It.Is<string>(x => appId.Equals(x)),
                It.Is<string>(x => recipient.RecipientId.Equals(x))));
            this.chatsService.Verify(x => x.GetChatThreadIdAsync(
                It.Is<string>(x => recipient.RecipientId.Equals(x)),
                It.Is<string>(x => appId.Equals(x))));
            this.sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(
                It.Is<SentNotificationDataEntity>(x => recipient.RecipientId.Equals(
                    x.RecipientId) &&
                    chatId.Equals(x.ConversationId) &&
                    serviceUrl.Equals(x.ServiceUrl))));
        }

        /// <summary>
        /// ArgumentNullException test.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task ArgumentNullExceptionTest()
        {
            // Arrange
            var activityContext = this.GetTeamsConversationActivity();
            string notificationId = "notificationid";
            SentNotificationDataEntity recipient = new SentNotificationDataEntity();

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((null /*notificationId*/, recipient), this.logger.Object);
            Func<Task> task1 = async () => await activityContext.CreateConversationAsync((notificationId, null /*recipient*/), this.logger.Object);
            Func<Task> task2 = async () => await activityContext.CreateConversationAsync((notificationId, recipient), null /*log*/);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
            await task1.Should().ThrowAsync<ArgumentNullException>("recipient is null");
            await task2.Should().ThrowAsync<ArgumentNullException>("log is null");
        }

        /// <summary>
        /// Initializes a new mock instance of the <see cref="TeamsConversationActivity"/> class.
        /// </summary>
        private TeamsConversationActivity GetTeamsConversationActivity(bool proactivelyInstallUserApp = false)
        {
            return new TeamsConversationActivity(
                this.conversationService.Object,
                this.sentNotificationDataRepository.Object,
                this.userDataRepository.Object,
                this.notificationDataRepository.Object,
                this.appManagerService.Object,
                this.chatsService.Object,
                this.appSettingsService.Object,
                Options.Create(new TeamsConversationOptions()
                {
                    ProactivelyInstallUserApp = proactivelyInstallUserApp,
                    MaxAttemptsToCreateConversation = this.maxAttempts,
                }),
                this.localizer.Object);
        }
    }
}
