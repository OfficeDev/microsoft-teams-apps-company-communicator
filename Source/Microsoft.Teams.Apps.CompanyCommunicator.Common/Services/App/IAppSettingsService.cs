// <copyright file="IAppSettingsService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// App settings interface.
    /// </summary>
    public interface IAppSettingsService
    {
        /// <summary>
        /// Gets cached user app id.
        /// </summary>
        /// <returns>User app id.</returns>
        public Task<string> GetUserAppIdAsync();

        /// <summary>
        /// Gets cached service url.
        /// </summary>
        /// <returns>Service url.</returns>
        public Task<string> GetServiceUrlAsync();

        /// <summary>
        /// Persists the user app id in database.
        /// </summary>
        /// <param name="userAppId">User app id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SetUserAppIdAsync(string userAppId);

        /// <summary>
        /// Persists the service url in database.
        /// </summary>
        /// <param name="serviceUrl">Service url.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SetServiceUrlAsync(string serviceUrl);

        /// <summary>
        /// Deletes the user app id from database if it exists, no-op otherwise.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task DeleteUserAppIdAsync();
    }
}
