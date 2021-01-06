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
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// Teams Bot service to create user conversation.
    /// </summary>
    public class ConversationService : IConversationService
    {
        private static readonly string MicrosoftTeamsChannelId = "msteams";

        private readonly BotFrameworkHttpAdapter botAdapter;
        private readonly UserAppCredentials userAppCredentials;
        private readonly AuthorAppCredentials authorAppCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationService"/> class.
        /// </summary>
        /// <param name="botAdapter">The bot adapter.</param>
        /// <param name="userAppCredentials">The user Microsoft app credentials.</param>
        /// <param name="authorAppCredentials">The author Microsoft app credentials.</param>
        public ConversationService(
            BotFrameworkHttpAdapter botAdapter,
            UserAppCredentials userAppCredentials,
            AuthorAppCredentials authorAppCredentials)
        {
            this.botAdapter = botAdapter ?? throw new ArgumentNullException(nameof(botAdapter));
            this.userAppCredentials = userAppCredentials ?? throw new ArgumentNullException(nameof(userAppCredentials));
            this.authorAppCredentials = authorAppCredentials ?? throw new ArgumentNullException(nameof(authorAppCredentials));
        }

        /// <inheritdoc/>
        public async Task<CreateConversationResponse> CreateUserConversationAsync(
            string teamsUserId,
            string tenantId,
            string serviceUrl,
            int maxAttempts,
            ILogger log)
        {
            return await this.CreateConversationAsync(teamsUserId, tenantId, serviceUrl, maxAttempts, this.userAppCredentials, log);
        }

        /// <inheritdoc/>
        public async Task<CreateConversationResponse> CreateAuthorConversationAsync(
            string teamsUserId,
            string tenantId,
            string serviceUrl,
            int maxAttempts,
            ILogger log)
        {
            return await this.CreateConversationAsync(teamsUserId, tenantId, serviceUrl, maxAttempts, this.authorAppCredentials, log);
        }

        private async Task<CreateConversationResponse> CreateConversationAsync(
            string teamsUserId,
            string tenantId,
            string serviceUrl,
            int maxAttempts,
            MicrosoftAppCredentials credentials,
            ILogger log)
        {
            if (string.IsNullOrEmpty(teamsUserId))
            {
                throw new ArgumentException($"'{nameof(teamsUserId)}' cannot be null or empty", nameof(teamsUserId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException($"'{nameof(tenantId)}' cannot be null or empty", nameof(tenantId));
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

            var response = new CreateConversationResponse();
            try
            {
                var retryPolicy = this.GetRetryPolicy(maxAttempts, log);
                await retryPolicy.ExecuteAsync(async () =>
                    await this.botAdapter.CreateConversationAsync(
                        channelId: ConversationService.MicrosoftTeamsChannelId,
                        serviceUrl: serviceUrl,
                        credentials: credentials,
                        conversationParameters: conversationParameters,
                        callback: (turnContext, cancellationToken) =>
                        {
                            // Success.
                            response.Result = Result.Succeeded;
                            response.StatusCode = (int)HttpStatusCode.Created;
                            response.ConversationId = turnContext.Activity.Conversation.Id;

                            return Task.CompletedTask;
                        },
                        cancellationToken: CancellationToken.None));
            }
            catch (ErrorResponseException e)
            {
                var errorMessage = $"{e.GetType()}: {e.Message}";
                log.LogError(e, $"Failed to create conversation. Exception message: {errorMessage}");

                var statusCode = e.Response.StatusCode;
                response.StatusCode = (int)statusCode;
                response.ErrorMessage = e.Response.Content;
                response.Result = statusCode == HttpStatusCode.TooManyRequests ? Result.Throttled : Result.Failed;
            }

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

                    // Handle throttling.
                    return e.Response.StatusCode == HttpStatusCode.TooManyRequests;
                })
                .WaitAndRetryAsync(delay);
        }
    }
}
