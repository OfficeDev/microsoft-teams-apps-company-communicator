// <copyright file="SendFileCardActivityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Activities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Adapter;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Secrets;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities;
    using Moq;
    using Xunit;

    /// <summary>
    /// SendFileCardActivity test class.
    /// </summary>
    public class SendFileCardActivityTest
    {
        private readonly Mock<IOptions<BotOptions>> botOptions = new Mock<IOptions<BotOptions>>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<IConversationService> conversationService = new Mock<IConversationService>();
        private readonly Mock<IOptions<TeamsConversationOptions>> options = new Mock<IOptions<TeamsConversationOptions>>();
        private readonly Mock<INotificationDataRepository> notificationDataRepository = new Mock<INotificationDataRepository>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> log = new Mock<ILogger>();
        private readonly Mock<ITurnContext> turnContext = new Mock<ITurnContext>();
        private readonly Mock<CCBotAdapter> botAdapter = new Mock<CCBotAdapter>(new Mock<ICertificateProvider>().Object, new Mock<BotFrameworkAuthentication>().Object);

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            this.botOptions.Setup(x => x.Value).Returns(new BotOptions() { AuthorAppId = "AuthorAppId" });
            this.options.Setup(x => x.Value).Returns(new TeamsConversationOptions() { MaxAttemptsToCreateConversation = 10 });
            Action action = () => new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.conversationService.Object, this.options.Object, this.notificationDataRepository.Object, this.localizer.Object);

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
            Action action1 = () => new SendFileCardActivity(null/*botOptions*/, this.botAdapter.Object, this.userDataRepository.Object, this.conversationService.Object, this.options.Object, this.notificationDataRepository.Object, this.localizer.Object);
            Action action2 = () => new SendFileCardActivity(this.botOptions.Object, null/*botAdapter*/, this.userDataRepository.Object, this.conversationService.Object, this.options.Object, this.notificationDataRepository.Object, this.localizer.Object);
            Action action3 = () => new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, null/*userDataRepository*/, this.conversationService.Object, this.options.Object, this.notificationDataRepository.Object, this.localizer.Object);
            Action action4 = () => new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, null/*conversationService*/, this.options.Object, this.notificationDataRepository.Object, this.localizer.Object);
            Action action5 = () => new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.conversationService.Object, null/*options*/, this.notificationDataRepository.Object, this.localizer.Object);
            Action action6 = () => new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.conversationService.Object, this.options.Object, null/*notificationDataRepository*/, this.localizer.Object);
            Action action7 = () => new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.conversationService.Object, this.options.Object, this.notificationDataRepository.Object, null/*localizer*/);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("botOptions is null.");
            action2.Should().Throw<ArgumentNullException>("botAdapter is null.");
            action3.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action4.Should().Throw<ArgumentNullException>("conversationService is null.");
            action5.Should().Throw<ArgumentNullException>("options is null.");
            action6.Should().Throw<ArgumentNullException>("notificationDataRepository is null.");
            action7.Should().Throw<ArgumentNullException>("localizer is null.");
        }

        /// <summary>
        /// Test case for null conversationId throws application exception.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversation_NullConversationId_ThrowsApplicationException()
        {
            // Arrange
            var activityInstance = this.GetSendFileCardActivityInstance();
            var user = this.GetUserData();
            var createConversationResponse = new CreateConversationResponse() { Result = Result.Succeeded, ConversationId = string.Empty, ErrorMessage = "errorMessage" };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
            this.conversationService.Setup(x => x.CreateAuthorConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.log.Object)).ReturnsAsync(createConversationResponse);

            string conversationThrottled = "FailedToCreateConversationThrottledFormt";
            var conversationThrottledText = new LocalizedString(conversationThrottled, conversationThrottled);
            this.localizer.Setup(_ => _[conversationThrottled]).Returns(conversationThrottledText);

            string conversationError = "FailedToCreateConversationFormat";
            var conversationErrorText = new LocalizedString(conversationError, conversationError);
            this.localizer.Setup(_ => _[conversationError]).Returns(conversationErrorText);

            this.userDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserDataEntity>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityInstance.SendFileCardActivityAsync((It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), this.log.Object);

            // Assert
            await task.Should().ThrowAsync<ApplicationException>();
        }

        /// <summary>
        /// Test case to verify the SaveWarningInNotification should be invoked once.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Call_SaveWarningInNotificationRepository_ShouldInvokedOnce()
        {
            // Arrange
            var activityInstance = this.GetSendFileCardActivityInstance();
            var user = this.GetUserData();
            var createConversationResponse = new CreateConversationResponse() { Result = Result.Throttled, ConversationId = string.Empty, ErrorMessage = "errorMessage" };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
            this.conversationService.Setup(x => x.CreateAuthorConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.log.Object)).ReturnsAsync(createConversationResponse);

            string conversationThrottled = "FailedToCreateConversationThrottledFormt";
            var conversationThrottledText = new LocalizedString(conversationThrottled, conversationThrottled);
            this.localizer.Setup(_ => _[conversationThrottled]).Returns(conversationThrottledText);

            string conversationForUser = "FailedToCreateConversationForUserFormat";
            var conversationForUserText = new LocalizedString(conversationForUser, conversationForUser);
            this.localizer.Setup(_ => _[conversationForUser]).Returns(conversationForUserText);

            this.userDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserDataEntity>())).Returns(Task.CompletedTask);
            this.notificationDataRepository.Setup(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityInstance.SendFileCardActivityAsync((It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), this.log.Object);

            // Assert
            await task.Should().ThrowAsync<Exception>();
            this.notificationDataRepository.Verify(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case to verify null conversationId to be returned for failed status of conversation response.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task CreateConversation_WithStatusFailed_ReturnsNullConversationId()
        {
            // Arrange
            var activityInstance = this.GetSendFileCardActivityInstance();
            var user = this.GetUserData();
            var createConversationResponse = new CreateConversationResponse() { Result = Result.Failed, ConversationId = string.Empty, ErrorMessage = "errorMessage" };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
            this.conversationService.Setup(x => x.CreateAuthorConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.log.Object)).ReturnsAsync(createConversationResponse);

            string conversationError = "FailedToCreateConversationFormat";
            var conversationErrorText = new LocalizedString(conversationError, conversationError);
            this.localizer.Setup(_ => _[conversationError]).Returns(conversationErrorText);

            string conversationForUser = "FailedToCreateConversationForUserFormat";
            var conversationForUserText = new LocalizedString(conversationForUser, conversationForUser);
            this.localizer.Setup(_ => _[conversationForUser]).Returns(conversationForUserText);

            this.userDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserDataEntity>())).Returns(Task.CompletedTask);
            this.notificationDataRepository.Setup(x => x.SaveWarningInNotificationDataEntityAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await activityInstance.SendFileCardActivityAsync((It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), this.log.Object);

            // Assert
            await task.Should().ThrowAsync<ApplicationException>();
            Assert.Null(user.ConversationId);
        }

        /// <summary>
        /// Test case to verify SendFileCard returns not null consentId for valid input.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task SendFileCard_ValidInput_ReturnsConsentId()
        {
            // Arrange
            var activityInstance = this.GetSendFileCardActivityInstance();
            var user = this.GetUserData();
            var resourceResponse = new ResourceResponse() { Id = "id" };
            var createConversationResponse = new CreateConversationResponse() { Result = Result.Succeeded, ConversationId = "conversationId", ErrorMessage = "errorMessage" };
            this.userDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
            this.conversationService.Setup(x => x.CreateAuthorConversationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.log.Object)).ReturnsAsync(createConversationResponse);
            this.botAdapter.Setup(x => x.ContinueConversationAsync(It.IsAny<string>(), It.IsAny<ConversationReference>(), It.IsAny<BotCallbackHandler>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            string description = "FileCardDescription";
            var descriptionText = new LocalizedString(description, description);
            this.localizer.Setup(_ => _[description]).Returns(descriptionText);

            this.userDataRepository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserDataEntity>())).Returns(Task.CompletedTask);
            this.turnContext.Setup(x => x.SendActivityAsync(It.IsAny<IActivity>(), It.IsAny<CancellationToken>())).ReturnsAsync(resourceResponse);

            // Act
            var result = await activityInstance.SendFileCardActivityAsync((It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), this.log.Object);

            // Assert
            Assert.NotNull(result);
        }

        private UserDataEntity GetUserData()
        {
            return new UserDataEntity() { ServiceUrl = "https://testServiceUrl.com", ConversationId = "conversationId", UserId = "userId" };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendFileCardActivity"/> class.
        /// </summary>
        /// <returns>return the instance of GetMetadataActivity.</returns>
        private SendFileCardActivity GetSendFileCardActivityInstance()
        {
            this.botOptions.Setup(x => x.Value).Returns(new BotOptions() { AuthorAppId = "AuthorAppId" });
            this.options.Setup(x => x.Value).Returns(new TeamsConversationOptions() { MaxAttemptsToCreateConversation = 10 });
            return new SendFileCardActivity(this.botOptions.Object, this.botAdapter.Object, this.userDataRepository.Object, this.conversationService.Object, this.options.Object, this.notificationDataRepository.Object, this.localizer.Object);
        }
    }
}