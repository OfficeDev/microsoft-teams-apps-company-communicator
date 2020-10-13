// <copyright file="AppManagerService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    extern alias BetaLib;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Graph;
    using Beta = BetaLib::Microsoft.Graph;

    /// <summary>
    /// Manage Teams Apps for a user or a team.
    /// </summary>
    internal class AppManagerService : IAppManagerService
    {
        private readonly Beta.IGraphServiceClient betaServiceClient;
        private readonly IGraphServiceClient serviceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppManagerService"/> class.
        /// </summary>
        /// <param name="betaServiceClient">Beta Graph service client.</param>
        /// <param name="serviceClient">V1 Graph service client.</param>
        internal AppManagerService(
            Beta.IGraphServiceClient betaServiceClient,
            IGraphServiceClient serviceClient)
        {
            this.betaServiceClient = betaServiceClient ?? throw new ArgumentNullException(nameof(betaServiceClient));
            this.serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        }

        /// <inheritdoc/>
        public async Task InstallAppForUserAsync(string appId, string userId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var userScopeTeamsAppInstallation = new Beta.UserScopeTeamsAppInstallation
            {
                AdditionalData = new Dictionary<string, object>()
                {
                    { "teamsApp@odata.bind", $"{GraphConstants.BetaBaseUrl}/appCatalogs/teamsApps/{appId}" },
                },
            };

            await this.betaServiceClient.Users[userId]
                .Teamwork
                .InstalledApps
                .Request()
                .WithMaxRetry(GraphConstants.MaxRetry)
                .AddAsync(userScopeTeamsAppInstallation);
        }

        /// <inheritdoc/>
        public async Task InstallAppForTeamAsync(string appId, string teamId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(teamId))
            {
                throw new ArgumentNullException(nameof(teamId));
            }

            var userScopeTeamsAppInstallation = new TeamsAppInstallation()
            {
                AdditionalData = new Dictionary<string, object>()
                {
                    { "teamsApp@odata.bind", $"{GraphConstants.V1BaseUrl}/appCatalogs/teamsApps/{appId}" },
                },
            };

            await this.serviceClient.Teams[teamId]
                .InstalledApps
                .Request()
                .WithMaxRetry(GraphConstants.MaxRetry)
                .AddAsync(userScopeTeamsAppInstallation);
        }

        /// <inheritdoc/>
        public async Task<bool> IsAppInstalledForUserAsync(string appId, string userId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var pagedApps = await this.betaServiceClient.Users[userId]
                .Teamwork
                .InstalledApps
                .Request()
                .Expand("teamsApp")
                .Filter($"teamsApp/id eq '{appId}'")
                .WithMaxRetry(GraphConstants.MaxRetry)
                .GetAsync();

            return pagedApps.CurrentPage.Any();
        }

        /// <inheritdoc/>
        public async Task<bool> IsAppInstalledForTeamAsync(string appId, string teamId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(teamId))
            {
                throw new ArgumentNullException(nameof(teamId));
            }

            var pagedApps = await this.serviceClient.Teams[teamId]
                .InstalledApps
                .Request()
                .Expand("teamsApp")
                .Filter($"teamsApp/id eq '{appId}'")
                .WithMaxRetry(GraphConstants.MaxRetry)
                .GetAsync();

            return pagedApps.CurrentPage.Any();
        }

        /// <inheritdoc/>
        public async Task<string> GetAppInstallationIdForUserAsync(string appId, string userId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var collection = await this.betaServiceClient.Users[userId]
                .Teamwork
                .InstalledApps
                .Request()
                .Expand("teamsApp")
                .Filter($"teamsApp/id eq '{appId}'")
                .WithMaxRetry(GraphConstants.MaxRetry)
                .GetAsync();

            return collection?.FirstOrDefault().Id;
        }

        /// <inheritdoc/>
        public async Task<string> GetAppInstallationIdForTeamAsync(string appId, string teamId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(teamId))
            {
                throw new ArgumentNullException(nameof(teamId));
            }

            var collection = await this.serviceClient.Teams[teamId]
                .InstalledApps
                .Request()
                .Expand("teamsApp")
                .Filter($"teamsApp/id eq '{appId}'")
                .WithMaxRetry(GraphConstants.MaxRetry)
                .GetAsync();

            return collection?.FirstOrDefault().Id;
        }
    }
}
