// <copyright file="SendFunctionTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Test
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    /// <summary>
    /// SendFunction test class.
    /// </summary>
    public class SendFunctionTest
    {
        private readonly Mock<INotificationService> notificationService = new Mock<INotificationService>();
        private readonly Mock<ISendingNotificationDataRepository> notificationRepo = new Mock<ISendingNotificationDataRepository>();
        private readonly Mock<IMessageService> messageService = new Mock<IMessageService>();
        private readonly Mock<ISendQueue> sendQueue = new Mock<ISendQueue>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();
        private readonly int deliveryCount = 0;
        private readonly DateTime dateTime = DateTime.Now;
        private IOptions<SendFunctionOptions> options = Options.Create(new SendFunctionOptions() { MaxNumberOfAttempts = 2, SendRetryDelayNumberOfSeconds = 300 });

        /// <summary>
        /// Constructor tests.
        /// </summary>
        [Fact]
        public void SendFunctionConstructorTest()
        {
            // Arrange
            Action action1 = () => new SendFunction(null /*options*/, this.notificationService.Object, this.messageService.Object, this.notificationRepo.Object, this.sendQueue.Object, this.localizer.Object);
            Action action2 = () => new SendFunction(this.options, null /*notificationService*/, this.messageService.Object, this.notificationRepo.Object, this.sendQueue.Object, this.localizer.Object);
            Action action3 = () => new SendFunction(this.options, this.notificationService.Object, null /*messageService*/, this.notificationRepo.Object, this.sendQueue.Object, this.localizer.Object);
            Action action4 = () => new SendFunction(this.options, this.notificationService.Object, this.messageService.Object, null /*notificationRepo*/, this.sendQueue.Object, this.localizer.Object);
            Action action5 = () => new SendFunction(this.options, this.notificationService.Object, this.messageService.Object, this.notificationRepo.Object, null /*sendQueue*/, this.localizer.Object);
            Action action6 = () => new SendFunction(this.options, this.notificationService.Object, this.messageService.Object, this.notificationRepo.Object, this.sendQueue.Object, null /*localizer*/);
            Action action7 = () => new SendFunction(this.options, this.notificationService.Object, this.messageService.Object, this.notificationRepo.Object, this.sendQueue.Object, this.localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("options is null.");
            action2.Should().Throw<ArgumentNullException>("notificationService is null.");
            action3.Should().Throw<ArgumentNullException>("messageService is null.");
            action4.Should().Throw<ArgumentNullException>("notificationRepo is null.");
            action5.Should().Throw<ArgumentNullException>("sendQueue is null.");
            action6.Should().Throw<ArgumentNullException>("localizer is null.");
            action7.Should().NotThrow();
        }

        /// <summary>
        /// Test for Pending Send Notification.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task PendingSendNotificationTest()
        {
            // Arrange
            // Notification is already sent or failed
            var sendFunctionInstance = this.GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\",\"UserType\":\"Member\"}}}";
            this.notificationService.Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>())).ReturnsAsync(false); // Notification is pending
            this.notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            this.notificationService.Verify(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()), Times.Once());
        }

        /// <summary>
        /// Test for send Notification with no ConversationId set.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationWhenNoConversationIdTest()
        {
            // Arrange
            var sendFunctionInstance = this.GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"\",\"UserType\":\"Member\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            this.notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            this.notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            this.notificationService.Verify(x => x.UpdateSentNotification(It.Is<string>(x => x.Equals("notificationId")), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test for send Notification in case of a guest user.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendFunc_GuestUser_ShouldNotSendMessage()
        {
            // Arrange
            var sendFunctionInstance = this.GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\",\"UserType\":\"Guest\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync();
            this.notificationService.Verify(x => x.UpdateSentNotification(It.Is<string>(x => x.Equals("notificationId")), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test for send Notification when userType is set as null.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendFunc_NullUserType_ShouldNotThrowException()
        {
            // Arrange
            var sendFunctionInstance = this.GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\",\"UserType\":\"\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync();
        }

        /// <summary>
        /// System is throttled. ReQueue send notification with delay.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Re_QueueSendNotificationWithDelayTest()
        {
            // Arrange
            // SendNotificationThrottled
            var sendFunctionInstance = this.GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\",\"UserType\":\"Member\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            this.notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            this.notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // mocking throttled.
            this.notificationService.Setup(x => x.IsSendNotificationThrottled()).ReturnsAsync(true);
            this.sendQueue.Setup(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            this.sendQueue.Verify(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()), Times.Once);
        }

        /// <summary>
        /// Send notifcatin success test.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationSuccess_Test()
        {
            // Arrange
            var adaptiveCardContent = "{\n  \"type\": \"AdaptiveCard\",\n  \"version\": \"1.0\",\n  \"body\": [\n    {\n      \"type\": \"TextBlock\",\n      \"size\": \"extraLarge\",\n      \"weight\": \"bolder\",\n      \"text\": \"hkhkj\",\n      \"wrap\": true\n    },\n    {\n      \"type\": \"TextBlock\",\n      \"text\": \"iuyuiy\",\n      \"wrap\": true\n    }\n  ],\n  \"msteams\": {\n    \"width\": \"full\"\n  }\n}";
            var sendFunctionInstance = this.GetSendFunction();
            SendMessageResponse sendMessageResponse = new SendMessageResponse()
            {
                ResultType = SendMessageResult.Succeeded,
            };
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\",\"UserType\":\"Member\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            this.notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            this.notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            this.notificationService.Setup(x => x.IsSendNotificationThrottled()).ReturnsAsync(false);
            this.sendQueue
                .Setup(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            var notificatioData = new SendingNotificationDataEntity() { Content = "{\"text\":\"Welcome\",\"displayText\":\"Hello\"}" };
            this.notificationRepo.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificatioData);
            this.notificationRepo.Setup(x => x.GetAdaptiveCardAsync(It.IsAny<string>())).Returns(Task.FromResult(adaptiveCardContent));
            this.messageService.Setup(x => x.SendMessageAsync(It.IsAny<IMessageActivity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.logger.Object)).ReturnsAsync(sendMessageResponse);
            this.notificationService.Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            this.messageService.Verify(x => x.SendMessageAsync(It.IsAny<IMessageActivity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.logger.Object));
        }

        /// <summary>
        /// Send Notification response is throttled then requeued.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationResponseThrottledTest()
        {
            // Arrange
            var adaptiveCardContent = "{\n  \"type\": \"AdaptiveCard\",\n  \"version\": \"1.0\",\n  \"body\": [\n    {\n      \"type\": \"TextBlock\",\n      \"size\": \"extraLarge\",\n      \"weight\": \"bolder\",\n      \"text\": \"hkhkj\",\n      \"wrap\": true\n    },\n    {\n      \"type\": \"TextBlock\",\n      \"text\": \"iuyuiy\",\n      \"wrap\": true\n    }\n  ],\n  \"msteams\": {\n    \"width\": \"full\"\n  }\n}";
            var sendFunctionInstance = this.GetSendFunction();
            SendMessageResponse sendMessageResponse = new SendMessageResponse()
            {
                ResultType = SendMessageResult.Throttled,
            };
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\",\"UserType\":\"Member\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            this.notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            this.notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            this.notificationService
                .Setup(x => x.IsSendNotificationThrottled())
                .ReturnsAsync(false);
            this.sendQueue
                .Setup(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            var notificatioData = new SendingNotificationDataEntity() { Content = "{\"text\":\"Welcome\",\"displayText\":\"Hello\"}" };
            this.notificationRepo.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(notificatioData);
            this.notificationRepo.Setup(x => x.GetAdaptiveCardAsync(It.IsAny<string>())).Returns(Task.FromResult(adaptiveCardContent));

            this.messageService.Setup(x => x.SendMessageAsync(It.IsAny<IMessageActivity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), this.logger.Object)).ReturnsAsync(sendMessageResponse);
            this.notificationService.Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            this.sendQueue.Verify(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()));
        }

        /// <summary>
        /// Test for Exception scenario in send notification.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationException_Test()
        {
            // Arrange
            var sendFunctionInstance = this.GetSendFunction();
            SendMessageResponse sendMessageResponse = new SendMessageResponse()
            {
                ResultType = SendMessageResult.Throttled,
            };
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : null }}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            this.notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            this.notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, this.deliveryCount, this.dateTime, string.Empty, this.logger.Object, new ExecutionContext());

            // Assert
            await task.Should().ThrowAsync<NullReferenceException>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        private SendFunction GetSendFunction()
        {
            return new SendFunction(this.options, this.notificationService.Object, this.messageService.Object, this.notificationRepo.Object, this.sendQueue.Object, this.localizer.Object);
        }
    }
}
