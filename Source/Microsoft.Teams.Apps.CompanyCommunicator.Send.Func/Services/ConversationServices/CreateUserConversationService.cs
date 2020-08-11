// <copyright file="CreateUserConversationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// Service for the bot to create user conversations.
    /// </summary>
    public class CreateUserConversationService
    {
        private static readonly string MicrosoftTeamsChannelId = "msteams";

        private readonly BotFrameworkHttpAdapter botAdapter;
        private readonly CommonMicrosoftAppCredentials appCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateUserConversationService"/> class.
        /// </summary>
        /// <param name="botAdapter">The bot adapter.</param>
        /// <param name="commonMicrosoftAppCredentials">The common Microsoft app credentials.</param>
        public CreateUserConversationService(
            BotFrameworkHttpAdapter botAdapter,
            CommonMicrosoftAppCredentials commonMicrosoftAppCredentials)
        {
            this.botAdapter = botAdapter;
            this.appCredentials = commonMicrosoftAppCredentials;
        }

        /// <summary>
        /// Creates a user conversation.
        /// </summary>
        /// <param name="userDataEntity">The data entity for the user for whom the conversation should be created.</param>
        /// <param name="maxNumberOfAttempts">The maximum number of request attempts to create the conversation.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<CreateUserConversationResponse> CreateConversationAsync(
            UserDataEntity userDataEntity,
            int maxNumberOfAttempts,
            ILogger log)
        {
            var createConversationResponse = new CreateUserConversationResponse();

            // Set the service URL in the trusted list to ensure the SDK includes the token in the request.
            MicrosoftAppCredentials.TrustServiceUrl(userDataEntity.ServiceUrl);

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
                            createConversationResponse.ResultType = CreateUserConversationResultType.Succeeded;
                            createConversationResponse.StatusCode = (int)HttpStatusCode.Created;
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
                    var errorMessage = $"{e.GetType()}: {e.Message}";
                    log.LogError(e, $"ERROR: {errorMessage}");

                    // This exception is thrown when a failure response is received when making the request
                    // to create the conversation.
                    var responseStatusCode = e.Response.StatusCode;
                    createConversationResponse.StatusCode = (int)responseStatusCode;

                    // If the response was a throttled status code or a 5xx status code,
                    // then delay and retry the request.
                    if (responseStatusCode == HttpStatusCode.TooManyRequests
                        || ((int)responseStatusCode >= 500 && (int)responseStatusCode < 600))
                    {
                        if (responseStatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // If the request was throttled, set the flag for indicating the throttled state.
                            createConversationResponse.ResultType = CreateUserConversationResultType.Throttled;
                        }
                        else
                        {
                            // If the request failed with a 5xx status code, set the flag for indicating the failure
                            // and store the content of the error message.
                            createConversationResponse.ResultType = CreateUserConversationResultType.Failed;
                            createConversationResponse.ErrorMessage = e.Response.Content;
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
