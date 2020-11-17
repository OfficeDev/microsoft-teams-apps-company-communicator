// <copyright file="AppSettingsService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;

    /// <summary>
    /// App settings service implementation.
    /// </summary>
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IAppConfigRepository repository;

        private string serviceUrl;
        private string userAppId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsService"/> class.
        /// </summary>
        /// <param name="repository">App configuration repository.</param>
        public AppSettingsService(IAppConfigRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceUrlAsync()
        {
            // check in-memory cache.
            if (!string.IsNullOrWhiteSpace(this.serviceUrl))
            {
                return this.serviceUrl;
            }

            var appConfig = await this.repository.GetAsync(
                AppConfigTableName.SettingsPartition,
                AppConfigTableName.ServiceUrlRowKey);

            this.serviceUrl = appConfig?.Value;
            return this.serviceUrl;
        }

        /// <inheritdoc/>
        public async Task<string> GetUserAppIdAsync()
        {
            // check in-memory cache.
            if (!string.IsNullOrWhiteSpace(this.userAppId))
            {
                return this.userAppId;
            }

            var appConfig = await this.repository.GetAsync(
                AppConfigTableName.SettingsPartition,
                AppConfigTableName.UserAppIdRowKey);

            this.userAppId = appConfig?.Value;
            return this.userAppId;
        }

        /// <inheritdoc/>
        public async Task SetServiceUrlAsync(string serviceUrl)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentNullException(nameof(serviceUrl));
            }

            var appConfig = new AppConfigEntity()
            {
                PartitionKey = AppConfigTableName.SettingsPartition,
                RowKey = AppConfigTableName.ServiceUrlRowKey,
                Value = serviceUrl,
            };

            await this.repository.InsertOrMergeAsync(appConfig);

            // Update in-memory cache.
            this.serviceUrl = serviceUrl;
        }

        /// <inheritdoc/>
        public async Task SetUserAppIdAsync(string userAppId)
        {
            if (string.IsNullOrWhiteSpace(userAppId))
            {
                throw new ArgumentNullException(nameof(userAppId));
            }

            var appConfig = new AppConfigEntity()
            {
                PartitionKey = AppConfigTableName.SettingsPartition,
                RowKey = AppConfigTableName.UserAppIdRowKey,
                Value = userAppId,
            };

            await this.repository.InsertOrMergeAsync(appConfig);

            // Update in-memory cache.
            this.userAppId = userAppId;
        }

        /// <inheritdoc/>
        public async Task DeleteUserAppIdAsync()
        {
            var appId = await this.GetUserAppIdAsync();
            if (string.IsNullOrEmpty(appId))
            {
                // User App id isn't cached.
                return;
            }

            var appConfig = new AppConfigEntity()
            {
                PartitionKey = AppConfigTableName.SettingsPartition,
                RowKey = AppConfigTableName.UserAppIdRowKey,
            };

            await this.repository.DeleteAsync(appConfig);

            // Clear in-memory cache.
            this.userAppId = null;
        }
    }
}
