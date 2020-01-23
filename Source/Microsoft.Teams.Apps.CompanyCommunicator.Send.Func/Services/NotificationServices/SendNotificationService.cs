// <copyright file="SendNotificationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Newtonsoft.Json;

    /// <summary>
    /// Service for the bot to manage sending notifications.
    /// </summary>
    public class SendNotificationService
    {
        private readonly string microsoftAppId;
        private readonly CommonBotAdapter botAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendNotificationService"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="commonBotAdapter">The common bot adapter.</param>
        public SendNotificationService(
            IOptions<BotOptions> botOptions,
            CommonBotAdapter commonBotAdapter)
        {
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.botAdapter = commonBotAdapter;
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="notificationContent">The content of the notification to be sent.</param>
        /// <param name="serviceUrl">The service URL to use for sending the notification.</param>
        /// <param name="conversationId">The conversation ID of the conversation to which the notification should be sent.</param>
        /// <param name="maxNumberOfAttempts">The maximum number of request attempts to send the notification.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<SendNotificationResponse> SendAsync(
            string notificationContent,
            string serviceUrl,
            string conversationId,
            int maxNumberOfAttempts)
        {
            var sendNotificationResponse = new SendNotificationResponse
            {
                NumberOfThrottleResponses = 0,
            };

            var conversationReference = new ConversationReference
            {
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = conversationId,
                },
            };

            await this.botAdapter.ContinueConversationAsync(
                botAppId: this.microsoftAppId,
                reference: conversationReference,
                callback: async (turnContext, cancellationToken) =>
                {
                    var adaptiveCardAttachment = new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(notificationContent),
                    };
                    var message = MessageFactory.Attachment(adaptiveCardAttachment);

                    // Loop through attempts to try and send the notification.
                    for (int i = 1; i <= maxNumberOfAttempts; i++)
                    {
                        try
                        {
                            await turnContext.SendActivityAsync(message);

                            // If made it passed the sending step, then the notification was sent successfully.
                            // Store the data about the successful request.
                            sendNotificationResponse.StatusCode = HttpStatusCode.Created;
                            sendNotificationResponse.ResultType = SendNotificationResultType.Succeeded;

                            break;
                        }
                        catch (ErrorResponseException e)
                        {
                            var responseStatusCode = e.Response.StatusCode;
                            sendNotificationResponse.StatusCode = responseStatusCode;

                            if (responseStatusCode == HttpStatusCode.TooManyRequests)
                            {
                                // If the request was throttled, set the flag for indicating the throttled state,
                                // increment the count of the number of throttles to be stored
                                // later, and if the maximum number of throttles has not been reached, delay
                                // for a bit of time to attempt the request again.
                                sendNotificationResponse.ResultType = SendNotificationResultType.Throttled;
                                sendNotificationResponse.NumberOfThrottleResponses++;

                                // Do not delay if already attempted the maximum number of attempts.
                                if (i < maxNumberOfAttempts)
                                {
                                    var random = new Random();
                                    await Task.Delay(random.Next(500, 1500));
                                }
                            }
                            else
                            {
                                // If in this block, then an error has occurred with the service.
                                // Return the failure and do not attempt the request again.
                                sendNotificationResponse.ResultType = SendNotificationResultType.Failed;
                                sendNotificationResponse.ErrorMessage = e.Response.Content;

                                break;
                            }
                        }
                    }
                },
                cancellationToken: CancellationToken.None);

            return sendNotificationResponse;
        }
    }
}
