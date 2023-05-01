// <copyright file="AppManagerService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Policies;

    /// <summary>
    /// Manage Teams Apps for a user or a team.
    /// </summary>
    internal class AppManagerService : IAppManagerService
    {
        private readonly IGraphServiceClient graphServiceClient;
        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppManagerService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">V1 Graph service client.</param>
        /// <param name="appConfiguration">App configuration.</param>
        internal AppManagerService(
            IGraphServiceClient graphServiceClient,
            IAppConfiguration appConfiguration)
        {
            this.graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
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

            var userScopeTeamsAppInstallation = new UserScopeTeamsAppInstallation
            {
                AdditionalData = new Dictionary<string, object>()
                {
                    { "teamsApp@odata.bind", $"{this.appConfiguration.GraphBaseUrl}/appCatalogs/teamsApps/{appId}" },
                },
            };

            var retryPolicy = PollyPolicy.GetGraphRetryPolicy(GraphConstants.MaxRetry);
            await retryPolicy.ExecuteAsync(async () =>
                await this.graphServiceClient.Users[userId]
                    .Teamwork
                    .InstalledApps
                    .Request()
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .AddAsync(userScopeTeamsAppInstallation));
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
                    { "teamsApp@odata.bind", $"{this.appConfiguration.GraphBaseUrl}/appCatalogs/teamsApps/{appId}" },
                },
            };

            var retryPolicy = PollyPolicy.GetGraphRetryPolicy(GraphConstants.MaxRetry);
            await retryPolicy.ExecuteAsync(async () =>
                await this.graphServiceClient.Teams[teamId]
                    .InstalledApps
                    .Request()
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .AddAsync(userScopeTeamsAppInstallation));
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

            var retryPolicy = PollyPolicy.GetGraphRetryPolicy(GraphConstants.MaxRetry);
            var pagedApps = await retryPolicy.ExecuteAsync(async () =>
                 await this.graphServiceClient.Users[userId]
                    .Teamwork
                    .InstalledApps
                    .Request()
                    .Expand("teamsApp")
                    .Filter($"teamsApp/id eq '{appId}'")
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync());

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

            var retryPolicy = PollyPolicy.GetGraphRetryPolicy(GraphConstants.MaxRetry);
            var pagedApps = await retryPolicy.ExecuteAsync(async () =>
                await this.graphServiceClient.Teams[teamId]
                    .InstalledApps
                    .Request()
                    .Expand("teamsApp")
                    .Filter($"teamsApp/id eq '{appId}'")
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync());

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

            var retryPolicy = PollyPolicy.GetGraphRetryPolicy(GraphConstants.MaxRetry);
            var collection = await retryPolicy.ExecuteAsync(async () =>
                await this.graphServiceClient.Users[userId]
                    .Teamwork
                    .InstalledApps
                    .Request()
                    .Expand("teamsApp")
                    .Filter($"teamsApp/id eq '{appId}'")
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync());

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

            var retryPolicy = PollyPolicy.GetGraphRetryPolicy(GraphConstants.MaxRetry);
            var collection = await retryPolicy.ExecuteAsync(async () =>
                await this.graphServiceClient.Teams[teamId]
                    .InstalledApps
                    .Request()
                    .Expand("teamsApp")
                    .Filter($"teamsApp/id eq '{appId}'")
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync());

            return collection?.FirstOrDefault().Id;
        }
    }
}
