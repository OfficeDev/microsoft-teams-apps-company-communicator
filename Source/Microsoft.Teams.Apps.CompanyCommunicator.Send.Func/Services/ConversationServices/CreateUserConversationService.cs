// <copyright file="CreateUserConversationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Newtonsoft.Json;

    /// <summary>
    /// Service for the bot to create user conversations.
    /// </summary>
    public class CreateUserConversationService
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateUserConversationService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpClient">The http client.</param>
        public CreateUserConversationService(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            this.configuration = configuration;
            this.httpClient = httpClient;
        }

        /// <summary>
        /// Creates a user conversation.
        /// </summary>
        /// <param name="userDataEntity">The data entity for the user for whom the conversation should be created.</param>
        /// <param name="botAccessToken">The bot access token.</param>
        /// <param name="maxNumberOfAttempts">The maximum number of request attempts to create the conversation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<CreateUserConversationResponse> CreateConversationAsync(
            UserDataEntity userDataEntity,
            string botAccessToken,
            int maxNumberOfAttempts)
        {
            var createConversationResponse = new CreateUserConversationResponse
            {
                NumberOfThrottleResponses = 0,
            };

            // Loop through attempts to try and create the conversation for the user.
            for (int i = 0; i < maxNumberOfAttempts; i++)
            {
                // Send a POST request to the correct URL with a valid access token and the
                // correct message body.
                var createConversationUrl = $"{userDataEntity.ServiceUrl}v3/conversations";
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, createConversationUrl))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botAccessToken);

                    var payloadString = "{\"bot\": { \"id\": \"28:" + this.configuration["MicrosoftAppId"] + "\"},\"isGroup\": false, \"tenantId\": \"" + userDataEntity.TenantId + "\", \"members\": [{\"id\": \"" + userDataEntity.UserId + "\"}]}";
                    requestMessage.Content = new StringContent(payloadString, Encoding.UTF8, "application/json");

                    using (var sendResponse = await this.httpClient.SendAsync(requestMessage))
                    {
                        createConversationResponse.StatusCode = sendResponse.StatusCode;

                        // If the conversation was created successfully, parse out the conversationId,
                        // store it for that user in the user data repository and place that
                        // conversationId for use when sending the notification to the user.
                        if (sendResponse.StatusCode == HttpStatusCode.Created)
                        {
                            var jsonResponseString = await sendResponse.Content.ReadAsStringAsync();
                            dynamic resp = JsonConvert.DeserializeObject(jsonResponseString);
                            var conversationId = resp.id;

                            createConversationResponse.ResultType = CreateUserConversationResultType.Succeeded;
                            createConversationResponse.ConversationId = conversationId;

                            break;
                        }
                        else if (sendResponse.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // If the request was throttled, set the flag for if the maximum number of attempts
                            // is reached, increment the count of the number of throttles to be stored
                            // later, and if the maximum number of throttles has not been reached, delay
                            // for a bit of time to attempt the request again.
                            createConversationResponse.ResultType = CreateUserConversationResultType.Throttled;
                            createConversationResponse.NumberOfThrottleResponses++;

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
                            createConversationResponse.ResultType = CreateUserConversationResultType.Failed;

                            break;
                        }
                    }
                }
            }

            return createConversationResponse;
        }
    }
}
