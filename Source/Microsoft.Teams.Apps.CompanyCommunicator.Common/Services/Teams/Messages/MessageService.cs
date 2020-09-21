// <copyright file="MessageService.cs" company="Microsoft">
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
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// Teams message service.
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly string microsoftAppId;
        private readonly BotFrameworkHttpAdapter botAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageService"/> class.
        /// </summary>
        /// <param name="botOptions">The bot options.</param>
        /// <param name="botAdapter">The bot adapter.</param>
        public MessageService(
            IOptions<BotOptions> botOptions,
            BotFrameworkHttpAdapter botAdapter)
        {
            this.microsoftAppId = botOptions?.Value?.MicrosoftAppId ?? throw new ArgumentNullException(nameof(botOptions));
            this.botAdapter = botAdapter ?? throw new ArgumentNullException(nameof(botAdapter));
        }

        /// <inheritdoc/>
        public async Task<SendMessageResponse> SendMessageAsync(
            IMessageActivity message,
            string conversationId,
            string serviceUrl,
            int maxAttempts,
            ILogger log)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException($"'{nameof(conversationId)}' cannot be null or empty", nameof(conversationId));
            }

            if (string.IsNullOrEmpty(serviceUrl))
            {
                throw new ArgumentException($"'{nameof(serviceUrl)}' cannot be null or empty", nameof(serviceUrl));
            }

            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

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

            var response = new SendMessageResponse
            {
                TotalNumberOfSendThrottles = 0,
                AllSendStatusCodes = string.Empty,
            };

            await this.botAdapter.ContinueConversationAsync(
                botAppId: this.microsoftAppId,
                reference: conversationReference,
                callback: async (turnContext, cancellationToken) =>
                {
                    for (int i = 1; i <= maxAttempts; i++)
                    {
                        try
                        {
                            // Send message.
                            await turnContext.SendActivityAsync(message);

                            // Success.
                            response.ResultType = SendMessageResult.Succeeded;
                            response.StatusCode = (int)HttpStatusCode.Created;
                            response.AllSendStatusCodes += $"{(int)HttpStatusCode.Created},";
                            break;
                        }
                        catch (ErrorResponseException e)
                        {
                            log.LogError(e, $"ERROR: {e.GetType()}: {e.Message}");

                            var responseStatusCode = e.Response.StatusCode;
                            response.StatusCode = (int)responseStatusCode;
                            response.AllSendStatusCodes += $"{(int)responseStatusCode},";

                            // If the response was a throttled status code or a 5xx status code,
                            // then delay and retry the request.
                            if (responseStatusCode == HttpStatusCode.TooManyRequests
                                || ((int)responseStatusCode >= 500 && (int)responseStatusCode < 600))
                            {
                                if (responseStatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    // If the request was throttled, set the flag for indicating the throttled state and
                                    // increment the count of the number of throttles to be stored later.
                                    response.ResultType = SendMessageResult.Throttled;
                                    response.TotalNumberOfSendThrottles++;
                                }
                                else
                                {
                                    // If the request failed with a 5xx status code, set the flag for indicating the failure
                                    // and store the content of the error message.
                                    response.ResultType = SendMessageResult.Failed;
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
                            else if (responseStatusCode == HttpStatusCode.NotFound)
                            {
                                // If in this block, then the recipient has been removed.
                                // This recipient should be excluded from the list.
                                response.ResultType = SendMessageResult.RecipientNotFound;
                                response.ErrorMessage = e.Response.Content;

                                break;
                            }
                            else
                            {
                                // If in this block, then an error has occurred with the service.
                                // Return the failure and do not attempt the request again.
                                response.ResultType = SendMessageResult.Failed;
                                response.ErrorMessage = e.Response.Content;

                                break;
                            }
                        }
                    }
                },
                cancellationToken: CancellationToken.None);

            return response;
        }
    }
}
