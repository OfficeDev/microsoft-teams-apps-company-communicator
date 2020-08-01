// <copyright file="SendNotificationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Newtonsoft.Json;

    /// <summary>
    /// Service for the bot to manage sending notifications.
    /// </summary>
    public class SendNotificationService
    {
        private static readonly string AdaptiveCardContentType = "application/vnd.microsoft.card.adaptive";

        private readonly string microsoftAppId;
        private readonly BotFrameworkHttpAdapter botAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendNotificationService"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="botAdapter">The bot adapter.</param>
        public SendNotificationService(
            IOptions<BotOptions> botOptions,
            BotFrameworkHttpAdapter botAdapter)
        {
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.botAdapter = botAdapter;
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="notificationContent">The content of the notification to be sent.</param>
        /// <param name="serviceUrl">The service URL to use for sending the notification.</param>
        /// <param name="conversationId">The conversation ID of the conversation to which the notification should be sent.</param>
        /// <param name="maxNumberOfAttempts">The maximum number of request attempts to send the notification.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<SendNotificationResponse> SendAsync(
            string notificationContent,
            string serviceUrl,
            string conversationId,
            int maxNumberOfAttempts,
            ILogger log)
        {
            var sendNotificationResponse = new SendNotificationResponse
            {
                TotalNumberOfSendThrottles = 0,
                AllSendStatusCodes = string.Empty,
            };

            // Set the service URL in the trusted list to ensure the SDK includes the token in the request.
            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

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
                        ContentType = SendNotificationService.AdaptiveCardContentType,
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
                            sendNotificationResponse.ResultType = SendNotificationResultType.Succeeded;
                            sendNotificationResponse.StatusCode = (int)HttpStatusCode.Created;
                            sendNotificationResponse.AllSendStatusCodes += $"{(int)HttpStatusCode.Created},";

                            break;
                        }
                        catch (ErrorResponseException e)
                        {
                            log.LogError(e, $"ERROR: {e.GetType()}: {e.Message}");

                            var responseStatusCode = e.Response.StatusCode;
                            sendNotificationResponse.StatusCode = (int)responseStatusCode;
                            sendNotificationResponse.AllSendStatusCodes += $"{(int)responseStatusCode},";

                            // If the response was a throttled status code or a 5xx status code,
                            // then delay and retry the request.
                            if (responseStatusCode == HttpStatusCode.TooManyRequests
                                || ((int)responseStatusCode >= 500 && (int)responseStatusCode < 600))
                            {
                                if (responseStatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    // If the request was throttled, set the flag for indicating the throttled state and
                                    // increment the count of the number of throttles to be stored later.
                                    sendNotificationResponse.ResultType = SendNotificationResultType.Throttled;
                                    sendNotificationResponse.TotalNumberOfSendThrottles++;
                                }
                                else
                                {
                                    // If the request failed with a 5xx status code, set the flag for indicating the failure
                                    // and store the content of the error message.
                                    sendNotificationResponse.ResultType = SendNotificationResultType.Failed;
                                    sendNotificationResponse.ErrorMessage = e.Response.Content;
                                }

                                // If the maximum number of attempts has not been reached, delay
                                // for a bit of time to attempt the request again.
                                // Do not delay if already attempted the maximum number of attempts.
                                if (i < maxNumberOfAttempts)
                                {
                                    var random = new Random();
                                    await Task.Delay(random.Next(500, 1500));
                                }
                            }
                            else if (responseStatusCode == HttpStatusCode.NotFound)
                            {
                                // If in this block, then the recipient has been removed.
                                // This recipient should be excluded from the list.
                                sendNotificationResponse.ResultType = SendNotificationResultType.RecipientNotFound;
                                sendNotificationResponse.ErrorMessage = e.Response.Content;

                                break;
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
