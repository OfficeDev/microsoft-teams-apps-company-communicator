// <copyright file="ChatsService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    extern alias BetaLib;

    using System;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    using Beta = BetaLib::Microsoft.Graph;

    /// <summary>
    /// Chats Service.
    /// </summary>
    internal class ChatsService : IChatsService
    {
        private readonly Beta.IGraphServiceClient betaServiceClient;
        private readonly IAppManagerService appManagerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatsService"/> class.
        /// </summary>
        /// <param name="betaServiceClient">Beta graph service client.</param>
        /// <param name="appManagerService">App manager service.</param>
        internal ChatsService(
            Beta.IGraphServiceClient betaServiceClient,
            IAppManagerService appManagerService)
        {
            this.betaServiceClient = betaServiceClient ?? throw new ArgumentNullException(nameof(betaServiceClient));
            this.appManagerService = appManagerService ?? throw new ArgumentNullException(nameof(appManagerService));
        }

        /// <inheritdoc/>
        public async Task<string> GetChatThreadIdAsync(string userId, string appId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            var installationId = await this.appManagerService.GetAppInstallationIdForUserAsync(appId, userId);
            var chat = await this.betaServiceClient.Users[userId]
                .Teamwork
                .InstalledApps[installationId]
                .Chat
                .Request()
                .WithMaxRetry(GraphConstants.MaxRetry)
                .GetAsync();

            return chat?.Id;
        }
    }
}
