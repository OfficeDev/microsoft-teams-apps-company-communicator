// <copyright file="IAppManagerService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System.Threading.Tasks;

    /// <summary>
    /// Manage Teams Apps for a user or a team.
    /// </summary>
    public interface IAppManagerService
    {
        /// <summary>
        /// Installs App from App catalog for a user.
        /// </summary>
        /// <param name="appId">Teams App Id.</param>
        /// <param name="userId">User's AAD Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task InstallAppForUserAsync(string appId, string userId);

        /// <summary>
        /// Installs App from App catalog for a team.
        /// </summary>
        /// <param name="appId">Teams App Id.</param>
        /// <param name="teamId">Team Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task InstallAppForTeamAsync(string appId, string teamId);

        /// <summary>
        /// Checks if the app is installed for a user.
        /// </summary>
        /// <param name="appId">Teams App Id.</param>
        /// <param name="userId">User Id.</param>
        /// <returns>Returns true if the app is installed, false otherwise.</returns>
        public Task<bool> IsAppInstalledForUserAsync(string appId, string userId);

        /// <summary>
        /// Checks if the app is installed for a team.
        /// </summary>
        /// <param name="appId">Teams App Id.</param>
        /// <param name="teamId">Team Id.</param>
        /// <returns>Returns true if the app is installed, false otherwise.</returns>
        public Task<bool> IsAppInstalledForTeamAsync(string appId, string teamId);

        /// <summary>
        /// Get Teams App Installation Id for a user.
        /// </summary>
        /// <param name="appId">Teams app id.</param>
        /// <param name="userId">User Id.</param>
        /// <returns>Teams App Installation Id.</returns>
        public Task<string> GetAppInstallationIdForUserAsync(string appId, string userId);

        /// <summary>
        /// Get Teams App Installation Id for a team.
        /// </summary>
        /// <param name="appId">Teams app id.</param>
        /// <param name="teamId">Team Id.</param>
        /// <returns>Teams App Installation Id.</returns>
        public Task<string> GetAppInstallationIdForTeamAsync(string appId, string teamId);
    }
}
