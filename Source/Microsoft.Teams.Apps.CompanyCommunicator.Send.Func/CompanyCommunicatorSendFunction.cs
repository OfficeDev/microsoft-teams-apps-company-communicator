// <copyright file="CompanyCommunicatorSendFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.DataServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.PrecheckServices;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for sending messages from the bot.
    /// </summary>
    public class CompanyCommunicatorSendFunction
    {
        /// <summary>
        /// This is set to 10 because the default maximum delivery count from the service bus
        /// message queue before the service bus will automatically put the message in the Dead Letter
        /// Queue is 10.
        /// </summary>
        private static readonly int MaxDeliveryCountForDeadLetter = 10;

        private readonly int maxNumberOfAttempts;
        private readonly double sendRetryDelayNumberOfSeconds;
        private readonly PrecheckService precheckService;
        private readonly GetSendNotificationParamsService getSendNotificationParamsService;
        private readonly SendNotificationService sendNotificationService;
        private readonly DelaySendingNotificationService delaySendingNotificationService;
        private readonly ManageResultDataService manageResultDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorSendFunction"/> class.
        /// </summary>
        /// <param name="companyCommunicatorSendFunctionOptions">The Company Communicator send function options.</param>
        /// <param name="precheckService">The service to precheck and determine if the queue message should be processed.</param>
        /// <param name="getSendNotificationParamsService">The service to get the parameters needed to send the notification.</param>
        /// <param name="sendNotificationService">The send notification service.</param>
        /// <param name="delaySendingNotificationService">The delay sending notification service.</param>
        /// <param name="manageResultDataService">The manage result data service.</param>
        public CompanyCommunicatorSendFunction(
            IOptions<CompanyCommunicatorSendFunctionOptions> companyCommunicatorSendFunctionOptions,
            PrecheckService precheckService,
            GetSendNotificationParamsService getSendNotificationParamsService,
            SendNotificationService sendNotificationService,
            DelaySendingNotificationService delaySendingNotificationService,
            ManageResultDataService manageResultDataService)
        {
            this.maxNumberOfAttempts = companyCommunicatorSendFunctionOptions.Value.MaxNumberOfAttempts;
            this.sendRetryDelayNumberOfSeconds = companyCommunicatorSendFunctionOptions.Value.SendRetryDelayNumberOfSeconds;
            this.precheckService = precheckService;
            this.getSendNotificationParamsService = getSendNotificationParamsService;
            this.sendNotificationService = sendNotificationService;
            this.delaySendingNotificationService = delaySendingNotificationService;
            this.manageResultDataService = manageResultDataService;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for sending messages from the bot.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="deliveryCount">The deliver count.</param>
        /// <param name="enqueuedTimeUtc">The enqueued time.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="log">The logger.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorSendFunction")]
        public async Task Run(
            [ServiceBusTrigger(
                SendQueue.QueueName,
                Connection = SendQueue.ServiceBusConnectionConfigurationKey)]
            string myQueueItem,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            var messageContent = JsonConvert.DeserializeObject<SendQueueMessageContent>(myQueueItem);

            try
            {
                /*
                 *
                 * Check if the queue message should be processed. If it should not be processed,
                 * then complete the function.
                 *
                 *
                 */

                var shouldProceedWithProcessing = await this.precheckService.VerifyMessageShouldBeProcessedAsync(
                    messageContent: messageContent,
                    sendRetryDelayNumberOfSeconds: this.sendRetryDelayNumberOfSeconds,
                    log: log);

                // If it is determined processing the queue message should not proceed, then
                // complete the function.
                if (!shouldProceedWithProcessing)
                {
                    return;
                }

                /*
                 *
                 * Use the information from the incoming message to generate the necessary parameters for
                 * sending the notification.
                 *
                 *
                 */

                var sendNotificationParams = await this.getSendNotificationParamsService
                    .GetSendNotificationParamsAsync(messageContent, log);

                // Stop the processing of the queue message if something negative occurred while generating
                // the parameters e.g. getting throttled, a failure, etc.
                if (sendNotificationParams.ForceCloseAzureFunction)
                {
                    return;
                }

                /*
                 *
                 * Send the notification.
                 *
                 *
                 */

                // Now that all of the necessary information is known, send the notification.
                var sendNotificationResponse = await this.sendNotificationService.SendAsync(
                    notificationContent: sendNotificationParams.NotificationContent,
                    serviceUrl: sendNotificationParams.ServiceUrl,
                    conversationId: sendNotificationParams.ConversationId,
                    maxNumberOfAttempts: this.maxNumberOfAttempts,
                    log: log);

                if (sendNotificationResponse.ResultType == SendNotificationResultType.Succeeded)
                {
                    log.LogInformation("MESSAGE SENT SUCCESSFULLY");

                    await this.manageResultDataService.ProcessResultDataAsync(
                        notificationId: messageContent.NotificationId,
                        recipientId: sendNotificationParams.RecipientId,
                        totalNumberOfSendThrottles: sendNotificationResponse.TotalNumberOfSendThrottles,
                        isStatusCodeFromCreateConversation: false,
                        statusCode: sendNotificationResponse.StatusCode,
                        allSendStatusCodes: sendNotificationResponse.AllSendStatusCodes,
                        errorMessage: sendNotificationResponse.ErrorMessage,
                        log: log);
                }
                else if (sendNotificationResponse.ResultType == SendNotificationResultType.Throttled)
                {
                    // If the request was attempted the maximum number of attempts and received
                    // all throttling responses, then set the overall delay time for the system so all
                    // other calls will be delayed and add the message back to the queue with a delay to be
                    // attempted later.
                    log.LogError($"MESSAGE THROTTLED. ERROR: {sendNotificationResponse.ErrorMessage}");

                    await this.delaySendingNotificationService
                        .DelaySendingNotificationAsync(
                            sendRetryDelayNumberOfSeconds: this.sendRetryDelayNumberOfSeconds,
                            sendQueueMessageContent: messageContent,
                            log: log);

                    // Ensure all processing of the queue message is stopped because of being delayed.
                    return;
                }
                else if (sendNotificationResponse.ResultType == SendNotificationResultType.RecipientNotFound)
                {
                    // If in this block, then the recipient must have been removed.
                    // Save the relevant information and exclude the not found recipient from the list.
                    log.LogError($"MESSAGE RECIPIENT NOT FOUND. ERROR: {sendNotificationResponse.ErrorMessage}");

                    await this.manageResultDataService.ProcessResultDataAsync(
                        notificationId: messageContent.NotificationId,
                        recipientId: sendNotificationParams.RecipientId,
                        totalNumberOfSendThrottles: sendNotificationResponse.TotalNumberOfSendThrottles,
                        isStatusCodeFromCreateConversation: false,
                        statusCode: sendNotificationResponse.StatusCode,
                        allSendStatusCodes: sendNotificationResponse.AllSendStatusCodes,
                        errorMessage: sendNotificationResponse.ErrorMessage,
                        log: log);
                }
                else if (sendNotificationResponse.ResultType == SendNotificationResultType.Failed)
                {
                    // If in this block, then an error has occurred with the service.
                    // Save the relevant information and do not attempt the request again.
                    log.LogError($"MESSAGE FAILED: {sendNotificationResponse.StatusCode}. ERROR: {sendNotificationResponse.ErrorMessage}");

                    await this.manageResultDataService.ProcessResultDataAsync(
                        notificationId: messageContent.NotificationId,
                        recipientId: sendNotificationParams.RecipientId,
                        totalNumberOfSendThrottles: sendNotificationResponse.TotalNumberOfSendThrottles,
                        isStatusCodeFromCreateConversation: false,
                        statusCode: sendNotificationResponse.StatusCode,
                        allSendStatusCodes: sendNotificationResponse.AllSendStatusCodes,
                        errorMessage: sendNotificationResponse.ErrorMessage,
                        log: log);

                    // Ensure all processing of the queue message is stopped because sending
                    // the notification failed.
                    return;
                }
            }
            catch (Exception e)
            {
                /*
                 * If in this block, then an exception was thrown. If the function throws an exception
                 * then the service bus message will be placed back on the queue. If this process has
                 * been done enough times and the message has been attempted to be delivered more than
                 * its allowed delivery count, then the message is placed on the dead letter queue of
                 * the service bus. For each attempt that did not result with the message being placed
                 * on the dead letter queue, set the status code to be stored as the FaultedAndRetryingStatusCode.
                 * If the maximum delivery count has been reached and the message will be placed on the
                 * dead letter queue, then set the status code to be stored as the FinalFaultedStatusCode.
                 */

                var errorMessage = $"{e.GetType()}: {e.Message}";

                log.LogError(e, $"ERROR: {errorMessage}");

                var statusCodeToStore = SentNotificationDataEntity.FaultedAndRetryingStatusCode;
                if (deliveryCount >= CompanyCommunicatorSendFunction.MaxDeliveryCountForDeadLetter)
                {
                    statusCodeToStore = SentNotificationDataEntity.FinalFaultedStatusCode;
                }

                // Set the status code in the allSendStatusCodes in order to store a record of
                // the attempt.
                await this.manageResultDataService.ProcessResultDataAsync(
                    notificationId: messageContent.NotificationId,
                    recipientId: messageContent.RecipientData.RecipientId,
                    totalNumberOfSendThrottles: 0,
                    isStatusCodeFromCreateConversation: false,
                    statusCode: statusCodeToStore,
                    allSendStatusCodes: $"{statusCodeToStore},",
                    errorMessage: errorMessage,
                    log: log);

                throw;
            }
        }
    }
}
