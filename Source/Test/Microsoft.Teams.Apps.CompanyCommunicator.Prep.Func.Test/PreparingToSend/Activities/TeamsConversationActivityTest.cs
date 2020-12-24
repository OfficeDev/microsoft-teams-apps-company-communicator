// <copyright file="TeamsConversationActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.PreparingToSend.Activities
{
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
    using System;
    using System.Threading.Tasks;
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
        private const int MaxAttempts = 10;

        /// <summary>
        /// Constructor tests.
        /// </summary> 
        [Fact]
        public void TeamsConversationActivityConstructorTest()
        {
            // Arrange
            Action action1 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, null /*userDataRepository*/, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action2 = () => new TeamsConversationActivity(null /*conversationService*/, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action3 = () => new TeamsConversationActivity(conversationService.Object, null /*sentNotificationDataRepository*/, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action4 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, null /*notificationDataRepository*/, appManagerService.Object, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action5 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, null /*appManagerService*/, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action6 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, null /*chatsService*/, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action7 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, null /*appSettingsService*/, Options.Create(new TeamsConversationOptions()), localizer.Object);
            Action action8 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), null /*localizer*/);
            Action action9 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, appSettingsService.Object, null /*options*/, localizer.Object);
            Action action10 = () => new TeamsConversationActivity(conversationService.Object, sentNotificationDataRepository.Object, userDataRepository.Object, notificationDataRepository.Object, appManagerService.Object, chatsService.Object, appSettingsService.Object, Options.Create(new TeamsConversationOptions()), localizer.Object);

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
            var activityContext = GetTeamsConversationActivity();
            var notificationId = "notificationId";
            SentNotificationDataEntity reciepient = new SentNotificationDataEntity()
            {
                RecipientType = SentNotificationDataEntity.TeamRecipientType
            };

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, reciepient), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
        }

        /// <summary>
        /// Success scenario to create conversation for users with teams user id
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversationAsync()
        {
            // Arrange
            string notificationId = "notificationId";
            string serviceUrl = "serviceUrlAppSettings";
            var activityContext = GetTeamsConversationActivity();
            SentNotificationDataEntity recipient = new SentNotificationDataEntity()
            {
                UserId = "userId",
                TenantId = "tenantId",
                ServiceUrl = "serviceUrl"
            };
            CreateConversationResponse response = new CreateConversationResponse()
            {
                Result = Result.Succeeded,
                ConversationId = "conversationid"
            };
            conversationService
                .Setup(x => x.CreateUserConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), logger.Object))
                .ReturnsAsync(response);
            appSettingsService
                .Setup(x => x.GetServiceUrlAsync())
                .Returns(Task.FromResult(serviceUrl));
            sentNotificationDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<SentNotificationDataEntity>()))
                .Returns(Task.CompletedTask);
            userDataRepository
                .Setup(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, recipient), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            conversationService.Verify(x => x.CreateUserConversationAsync(
                It.Is<string>(x => recipient.UserId.Equals(x)),
                It.Is<string>(x => recipient.TenantId.Equals(x)),
                It.Is<string>(x => recipient.ServiceUrl.Equals(x)),
                It.IsAny<int>(), logger.Object));
            userDataRepository.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => recipient.UserId.Equals(x.UserId))));
        }

        /// <summary>
        /// Conversation not created as Proactive app installation is disabled
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversationAsync_UserIdNullOrEmpty()
        {
            // Arrange
            var activityContext = GetTeamsConversationActivity(false/*proactivelyInstallUserApp*/);
            var notificationId = "notificationId";
            SentNotificationDataEntity recipient = new SentNotificationDataEntity()
            {
                UserId = string.Empty,
                RecipientId = "recipientId",
            };

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, recipient), logger.Object);

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
            var activityContext = GetTeamsConversationActivity(true/*proactivelyInstallUserApp*/);
            var notificationId = "notificationId";
            var appId = "appId";
            var chatId = "chatId";
            string serviceUrl = "serviceUrl";
            SentNotificationDataEntity recipient = new SentNotificationDataEntity()
            {
                UserId = string.Empty,
                RecipientId = "recipientId",
            };

            appSettingsService
                .Setup(x => x.GetUserAppIdAsync())
                .Returns(Task.FromResult(appId));
            appManagerService
                .Setup(x => x.InstallAppForUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            chatsService
                .Setup(x => x.GetChatThreadIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(chatId));
            appSettingsService
                .Setup(x => x.GetServiceUrlAsync())
                .Returns(Task.FromResult(serviceUrl));

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((notificationId, recipient), logger.Object);

            // Assert
            await task.Should().NotThrowAsync();
            appManagerService.Verify(x => x.InstallAppForUserAsync(
                It.Is<string>(x => appId.Equals(x)),
                It.Is<string>(x => recipient.RecipientId.Equals(x))));
            chatsService.Verify(x => x.GetChatThreadIdAsync(
                It.Is<string>(x => recipient.RecipientId.Equals(x)),
                It.Is<string>(x => appId.Equals(x))));
            sentNotificationDataRepository.Verify(x => x.InsertOrMergeAsync(
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
            var activityContext = GetTeamsConversationActivity();
            string notificationId = "notificationid";
            SentNotificationDataEntity recipient = new SentNotificationDataEntity();

            // Act
            Func<Task> task = async () => await activityContext.CreateConversationAsync((null /*notificationId*/, recipient), logger.Object);
            Func<Task> task1 = async () => await activityContext.CreateConversationAsync((notificationId, null /*recipient*/), logger.Object);
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
                conversationService.Object,
                sentNotificationDataRepository.Object,
                userDataRepository.Object,
                notificationDataRepository.Object,
                appManagerService.Object,
                chatsService.Object,
                appSettingsService.Object,
                Options.Create(new TeamsConversationOptions()
                {
                    ProactivelyInstallUserApp = proactivelyInstallUserApp,
                    MaxAttemptsToCreateConversation = MaxAttempts
                }),
                localizer.Object);
        }
    }
}
