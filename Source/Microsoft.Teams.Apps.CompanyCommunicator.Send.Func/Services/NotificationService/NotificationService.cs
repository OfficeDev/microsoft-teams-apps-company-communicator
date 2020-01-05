// <copyright file="NotificationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationService
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Newtonsoft.Json;

    /// <summary>
    /// Service for the bot to manage sending notifications.
    /// </summary>
    public class NotificationService
    {
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="httpClient">The http client.</param>
        public NotificationService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="notificationContent">The content of the notification to be sent.</param>
        /// <param name="serviceUrl">The service URL to use for sending the notification.</param>
        /// <param name="conversationId">The conversation ID of the conversation to which the notification should be sent.</param>
        /// <param name="botAccessToken">The bot access token.</param>
        /// <param name="maxNumberOfAttempts">The maximum number of request attempts to send the notification.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<SendNotificationResponse> SendNotificationAsync(
            string notificationContent,
            string serviceUrl,
            string conversationId,
            string botAccessToken,
            int maxNumberOfAttempts)
        {
            var sendNotificationResponse = new SendNotificationResponse
            {
                NumberOfThrottleResponses = 0,
            };

            // Loop through attempts to try and send the notification.
            for (int i = 0; i < maxNumberOfAttempts; i++)
            {
                // Send a POST request to the correct URL with a valid access token and the
                // correct message body.
                var conversationUrl = $"{serviceUrl}v3/conversations/{conversationId}/activities";
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, conversationUrl))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botAccessToken);

                    var attachmentJsonString = JsonConvert.DeserializeObject(notificationContent);
                    var messageString = "{ \"type\": \"message\", \"attachments\": [ { \"contentType\": \"application/vnd.microsoft.card.adaptive\", \"content\": " + attachmentJsonString + " } ] }";
                    requestMessage.Content = new StringContent(messageString, Encoding.UTF8, "application/json");

                    using (var sendResponse = await this.httpClient.SendAsync(requestMessage))
                    {
                        sendNotificationResponse.StatusCode = sendResponse.StatusCode;

                        // If the notification was sent successfully, store the data about the
                        // successful request.
                        if (sendResponse.StatusCode == HttpStatusCode.Created)
                        {
                            sendNotificationResponse.ResultType = SendNotificationResultType.Succeeded;

                            break;
                        }
                        else if (sendResponse.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // If the request was throttled, set the flag for if the maximum number of attempts
                            // is reached, increment the count of the number of throttles to be stored
                            // later, and if the maximum number of throttles has not been reached, delay
                            // for a bit of time to attempt the request again.
                            sendNotificationResponse.ResultType = SendNotificationResultType.Throttled;
                            sendNotificationResponse.NumberOfThrottleResponses++;

                            // Do not delay if already attempted the maximum number of attempts.
                            if (i != maxNumberOfAttempts - 1)
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

                            break;
                        }
                    }
                }
            }

            return sendNotificationResponse;
        }
    }
}
