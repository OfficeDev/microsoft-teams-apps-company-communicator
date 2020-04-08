// <copyright file="CreateUserConversationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// Service for the bot to create user conversations.
    /// </summary>
    public class CreateUserConversationService
    {
        private static readonly string MicrosoftTeamsChannelId = "msteams";

        private readonly CommonBotAdapter botAdapter;
        private readonly CommonMicrosoftAppCredentials appCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateUserConversationService"/> class.
        /// </summary>
        /// <param name="commonBotAdapter">The common bot adapter.</param>
        /// <param name="commonMicrosoftAppCredentials">The common Microsoft app credentials.</param>
        public CreateUserConversationService(
            CommonBotAdapter commonBotAdapter,
            CommonMicrosoftAppCredentials commonMicrosoftAppCredentials)
        {
            this.botAdapter = commonBotAdapter;
            this.appCredentials = commonMicrosoftAppCredentials;
        }

        /// <summary>
        /// Creates a user conversation.
        /// </summary>
        /// <param name="userDataEntity">The data entity for the user for whom the conversation should be created.</param>
        /// <param name="maxNumberOfAttempts">The maximum number of request attempts to create the conversation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<CreateUserConversationResponse> CreateConversationAsync(
            UserDataEntity userDataEntity,
            int maxNumberOfAttempts)
        {
            var createConversationResponse = new CreateUserConversationResponse
            {
                NumberOfThrottleResponses = 0,
            };

            // Create the conversation parameters that will be used when
            // creating the conversation for that user.
            var conversationParameters = new ConversationParameters
            {
                TenantId = userDataEntity.TenantId,
                Members = new ChannelAccount[]
                {
                    new ChannelAccount
                    {
                        Id = userDataEntity.UserId,
                    },
                },
            };

            // Loop through attempts to try and create the conversation for the user.
            var conversationCreatedSuccessfully = false;
            for (int i = 1; i <= maxNumberOfAttempts && !conversationCreatedSuccessfully; i++)
            {
                try
                {
                    await this.botAdapter.CreateConversationAsync(
                        channelId: CreateUserConversationService.MicrosoftTeamsChannelId,
                        serviceUrl: userDataEntity.ServiceUrl,
                        credentials: this.appCredentials,
                        conversationParameters: conversationParameters,
                        callback: (turnContext, cancellationToken) =>
                        {
                            // If this callback is used, that means the conversation was
                            // created successfully and the information will be in the Activity of
                            // the turnContext.
                            // Set the status code to indicate it was created, set that it was
                            // successfully created, and place that conversationId in the response for
                            // use when sending the notification to the user.
                            createConversationResponse.StatusCode = HttpStatusCode.Created;
                            createConversationResponse.ResultType = CreateUserConversationResultType.Succeeded;
                            createConversationResponse.ConversationId = turnContext.Activity.Conversation.Id;

                            // This is used to signal the conversation was created successfully and to
                            // "break" out of the loop in order to not make multiple attempts.
                            conversationCreatedSuccessfully = true;

                            return Task.CompletedTask;
                        },
                        cancellationToken: CancellationToken.None);
                }
                catch (ErrorResponseException e)
                {
                    // This exception is thrown when a failure response is received when making the request
                    // to create the conversation.
                    var responseStatusCode = e.Response.StatusCode;
                    createConversationResponse.StatusCode = responseStatusCode;

                    if (responseStatusCode == HttpStatusCode.TooManyRequests)
                    {
                        // If the request was throttled, set the flag for indicating the throttled state,
                        // increment the count of the number of throttles to be stored
                        // later, and if the maximum number of throttles has not been reached, delay
                        // for a bit of time to attempt the request again.
                        createConversationResponse.ResultType = CreateUserConversationResultType.Throttled;
                        createConversationResponse.NumberOfThrottleResponses++;

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
                        createConversationResponse.ResultType = CreateUserConversationResultType.Failed;
                        createConversationResponse.ErrorMessage = e.Response.Content;

                        break;
                    }
                }
            }

            return createConversationResponse;
        }
    }
}
