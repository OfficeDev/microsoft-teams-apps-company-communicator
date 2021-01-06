// <copyright file="NotificationServiceTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Test
{
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
    using System;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// SendFunction test class.
    /// </summary>
    public class SendFunctionTest
    {
        IOptions<SendFunctionOptions> options = Options.Create(new SendFunctionOptions() { MaxNumberOfAttempts = 2, SendRetryDelayNumberOfSeconds = 300 });
        private readonly Mock<INotificationService> notificationService = new Mock<INotificationService>();
        private readonly Mock<ISendingNotificationDataRepository> notificationRepo = new Mock<ISendingNotificationDataRepository>();
        private readonly Mock<IMessageService> messageService = new Mock<IMessageService>();
        private readonly Mock<ISendQueue> sendQueue = new Mock<ISendQueue>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();
        private readonly int deliveryCount = 0;
        

        /// <summary>
        /// Constructor tests.
        /// </summary> 
        [Fact]
        public void SendFunctionConstructorTest()
        {
            // Arrange
            Action action1 = () => new SendFunction(null /*options*/, notificationService.Object, messageService.Object, notificationRepo.Object, sendQueue.Object, localizer.Object);
            Action action2 = () => new SendFunction(options, null /*notificationService*/, messageService.Object, notificationRepo.Object, sendQueue.Object, localizer.Object);
            Action action3 = () => new SendFunction(options, notificationService.Object, null /*messageService*/, notificationRepo.Object, sendQueue.Object, localizer.Object);
            Action action4 = () => new SendFunction(options, notificationService.Object, messageService.Object, null /*notificationRepo*/, sendQueue.Object, localizer.Object);
            Action action5 = () => new SendFunction(options, notificationService.Object, messageService.Object, notificationRepo.Object, null /*sendQueue*/, localizer.Object);
            Action action6 = () => new SendFunction(options, notificationService.Object, messageService.Object, notificationRepo.Object, sendQueue.Object, null /*localizer*/);
            Action action7 = () => new SendFunction(options, notificationService.Object, messageService.Object, notificationRepo.Object, sendQueue.Object, localizer.Object);

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
            //Notification is already sent or failed
            var sendFunctionInstance = GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\"}";
            notificationService.Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>())).ReturnsAsync(false); //Notification is pending

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, deliveryCount, new DateTime(), string.Empty, logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<Exception>();
            notificationService.Verify(x=>x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()),Times.Once());
        }

        /// <summary>
        /// Test for send Notification with no ConversationId set.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationWhenNoConversationIdTest()
        {
            // Arrange
            var sendFunctionInstance = GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, deliveryCount, new DateTime(), string.Empty, logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            notificationService.Verify(x => x.UpdateSentNotification(It.Is<string>(x=>x.Equals("notificationId")), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        /// <summary>
        /// System is throttled. ReQueue send notification with delay.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Re_QueueSendNotificationWithDelayTest()
        {
            // Arrange
            //SendNotificationThrottled
            var sendFunctionInstance = GetSendFunction();
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // mocking throttled.
            notificationService.Setup(x => x.IsSendNotificationThrottled()).ReturnsAsync(true);
            sendQueue.Setup(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, deliveryCount, new DateTime(), string.Empty, logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            sendQueue.Verify(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()), Times.Once);
        }

        /// <summary>
        /// Send notifcatin success test.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationSuccess_Test()
        {
            // Arrange
            var sendFunctionInstance = GetSendFunction();
            SendMessageResponse sendMessageResponse = new SendMessageResponse()
            {
                ResultType = SendMessageResult.Succeeded,
            };
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            notificationService.Setup(x => x.IsSendNotificationThrottled()).ReturnsAsync(false);
            sendQueue
                .Setup(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            SendingNotificationDataEntity NotificatioData = new SendingNotificationDataEntity() { Content = "{\"text\":\"Welcome\",\"displayText\":\"Hello\"}" };
            notificationRepo.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(NotificatioData);
            messageService.Setup(x => x.SendMessageAsync(It.IsAny<IMessageActivity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), logger.Object)).ReturnsAsync(sendMessageResponse);
            notificationService.Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, deliveryCount, new DateTime(), string.Empty, logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            messageService.Verify(x => x.SendMessageAsync(It.IsAny<IMessageActivity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), logger.Object));
        }

        /// <summary>
        /// Send Notification response is throttled then requeued.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationResponseThrottledTest()
        {
            // Arrange
            var sendFunctionInstance = GetSendFunction();
            SendMessageResponse sendMessageResponse = new SendMessageResponse()
            {
                ResultType = SendMessageResult.Throttled
            };
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : \"TestResp\", \"UserData\": { \"UserId\" : \"userId\",\"ConversationId\":\"conversationId\"}}}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            notificationService
                .Setup(x => x.IsSendNotificationThrottled())
                .ReturnsAsync(false);
            sendQueue
                .Setup(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            SendingNotificationDataEntity NotificatioData = new SendingNotificationDataEntity() { Content = "{\"text\":\"Welcome\",\"displayText\":\"Hello\"}" };
            notificationRepo.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(NotificatioData);
            messageService.Setup(x => x.SendMessageAsync(It.IsAny<IMessageActivity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), logger.Object)).ReturnsAsync(sendMessageResponse);
            notificationService.Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, deliveryCount, new DateTime(), string.Empty, logger.Object, new ExecutionContext());

            // Assert
            await task.Should().NotThrowAsync<NullReferenceException>();
            sendQueue.Verify(x => x.SendDelayedAsync(It.IsAny<SendQueueMessageContent>(), It.IsAny<double>()));
        }

        /// <summary>
        /// Test for Exception scenario in send notification.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SendNotificationException_Test()
        {
            // Arrange
            var sendFunctionInstance = GetSendFunction();
            SendMessageResponse sendMessageResponse = new SendMessageResponse()
            {
                ResultType = SendMessageResult.Throttled
            };
            string data = "{\"NotificationId\":\"notificationId\",\"RecipientData\": {\"RecipientId\" : null }}";
            SendQueueMessageContent messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(data);
            notificationService
                .Setup(x => x.IsPendingNotification(It.IsAny<SendQueueMessageContent>()))
                .ReturnsAsync(true);
            notificationService
                .Setup(x => x.UpdateSentNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> task = async () => await sendFunctionInstance.Run(data, deliveryCount, new DateTime(), string.Empty, logger.Object, new ExecutionContext());

            // Assert
            await task.Should().ThrowAsync<NullReferenceException>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        private SendFunction GetSendFunction()
        {
            return new SendFunction(options, notificationService.Object, messageService.Object, notificationRepo.Object, sendQueue.Object, localizer.Object);
        }
    }
}
