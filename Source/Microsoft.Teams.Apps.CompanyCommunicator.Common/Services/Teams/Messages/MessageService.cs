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
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

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
            this.microsoftAppId = botOptions?.Value?.UserAppId ?? throw new ArgumentNullException(nameof(botOptions));
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
                    var policy = this.GetRetryPolicy(maxAttempts, log);
                    try
                    {
                        // Send message.
                        await policy.ExecuteAsync(async () => await turnContext.SendActivityAsync(message));

                        // Success.
                        response.ResultType = SendMessageResult.Succeeded;
                        response.StatusCode = (int)HttpStatusCode.Created;
                        response.AllSendStatusCodes += $"{(int)HttpStatusCode.Created},";
                    }
                    catch (ErrorResponseException e)
                    {
                        var errorMessage = $"{e.GetType()}: {e.Message}";
                        log.LogError(e, $"Failed to send message. Exception message: {errorMessage}");

                        response.StatusCode = (int)e.Response.StatusCode;
                        response.AllSendStatusCodes += $"{(int)e.Response.StatusCode},";
                        response.ErrorMessage = e.Response.Content;
                        switch (e.Response.StatusCode)
                        {
                            case HttpStatusCode.TooManyRequests:
                                response.ResultType = SendMessageResult.Throttled;
                                response.TotalNumberOfSendThrottles = maxAttempts;
                                break;

                            case HttpStatusCode.NotFound:
                                response.ResultType = SendMessageResult.RecipientNotFound;
                                break;

                            default:
                                response.ResultType = SendMessageResult.Failed;
                                break;
                        }
                    }
                },
                cancellationToken: CancellationToken.None);

            return response;
        }

        private AsyncRetryPolicy GetRetryPolicy(int maxAttempts, ILogger log)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: maxAttempts);
            return Policy
                .Handle<ErrorResponseException>(e =>
                {
                    var errorMessage = $"{e.GetType()}: {e.Message}";
                    log.LogError(e, $"Exception thrown: {errorMessage}");

                    // Handle throttling and internal server errors.
                    var statusCode = e.Response.StatusCode;
                    return statusCode == HttpStatusCode.TooManyRequests || ((int)statusCode >= 500 && (int)statusCode < 600);
                })
                .WaitAndRetryAsync(delay);
        }
    }
}
