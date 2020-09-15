// <copyright file="ConversationService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Teams
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// Teams Bot service to create user conversation.
    /// TODO(guptaa): move retry logic outside or pass as a parameter.
    /// </summary>
    public class ConversationService : IConversationService
    {
        private static readonly string MicrosoftTeamsChannelId = "msteams";

        private readonly BotFrameworkHttpAdapter botAdapter;
        private readonly CommonMicrosoftAppCredentials appCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationService"/> class.
        /// </summary>
        /// <param name="botAdapter">The bot adapter.</param>
        /// <param name="appCredentials">The common Microsoft app credentials.</param>
        public ConversationService(
            BotFrameworkHttpAdapter botAdapter,
            CommonMicrosoftAppCredentials appCredentials)
        {
            this.botAdapter = botAdapter ?? throw new ArgumentNullException(nameof(botAdapter));
            this.appCredentials = appCredentials ?? throw new ArgumentNullException(nameof(appCredentials));
        }

        /// <inheritdoc/>
        public async Task<CreateConversationResponse> CreateConversationAsync(
            string teamsUserId,
            string tenantId,
            string serviceUrl,
            int maxAttempts,
            ILogger log)
        {
            var response = new CreateConversationResponse();

            // Set the service URL in the trusted list to ensure the SDK includes the token in the request.
            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

            // Create the conversation parameters that will be used when
            // creating the conversation for that user.
            var conversationParameters = new ConversationParameters
            {
                TenantId = tenantId,
                Members = new ChannelAccount[]
                {
                    new ChannelAccount
                    {
                        Id = teamsUserId,
                    },
                },
            };

            // Loop through attempts to try and create the conversation for the user.
            var conversationCreatedSuccessfully = false;
            for (int i = 1; i <= maxAttempts && !conversationCreatedSuccessfully; i++)
            {
                try
                {
                    await this.botAdapter.CreateConversationAsync(
                        channelId: ConversationService.MicrosoftTeamsChannelId,
                        serviceUrl: serviceUrl,
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
                            response.Result = Result.Succeeded;
                            response.StatusCode = (int)HttpStatusCode.Created;
                            response.ConversationId = turnContext.Activity.Conversation.Id;

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
                    response.StatusCode = (int)responseStatusCode;

                    // If the response was a throttled status code or a 5xx status code,
                    // then delay and retry the request.
                    if (responseStatusCode == HttpStatusCode.TooManyRequests
                        || ((int)responseStatusCode >= 500 && (int)responseStatusCode < 600))
                    {
                        if (responseStatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // If the request was throttled, set the flag for indicating the throttled state.
                            response.Result = Result.Throttled;
                        }
                        else
                        {
                            // If the request failed with a 5xx status code, set the flag for indicating the failure
                            // and store the content of the error message.
                            response.Result = Result.Failed;
                            response.ErrorMessage = e.Response.Content;
                        }

                        // If the maximum number of attempts has not been reached, delay
                        // for a bit of time to attempt the request again.
                        // Do not delay if already attempted the maximum number of attempts.
                        if (i < maxAttempts)
                        {
                            var random = new Random();
                            await Task.Delay(random.Next(500, 1500));
                        }
                    }
                    else
                    {
                        // If in this block, then an error has occurred with the service.
                        // Return the failure and do not attempt the request again.
                        response.Result = Result.Failed;
                        response.ErrorMessage = e.Response.Content;

                        break;
                    }
                }
            }

            return response;
        }
    }
}
