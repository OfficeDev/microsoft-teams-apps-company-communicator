// <copyright file="ConfigurationCredentialProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This class implements ICredentialProvider, which is used by the bot framework to retrieve credential info.
    /// </summary>
    public class ConfigurationCredentialProvider : ICredentialProvider
    {
        private readonly Dictionary<string, string> credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationCredentialProvider"/> class.
        /// A constructor that accepts a map of bot id list and credentials.
        /// </summary>
        /// <param name="botOptions">bot options.</param>
        public ConfigurationCredentialProvider(IOptions<BotOptions> botOptions)
        {
            botOptions = botOptions ?? throw new ArgumentNullException(nameof(botOptions));
            this.credentials = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(botOptions.Value.UserAppId))
            {
                this.credentials.Add(botOptions.Value.UserAppId, botOptions.Value.UserAppPassword);
            }

            if (!string.IsNullOrEmpty(botOptions.Value.AuthorAppId))
            {
                this.credentials.Add(botOptions.Value.AuthorAppId, botOptions.Value.AuthorAppPassword);
            }
        }

        /// <summary>
        /// Validates an app ID.
        /// </summary>
        /// <param name="appId">The app ID to validate.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful, the result is true if <paramref name="appId"/>
        /// is valid for the controller; otherwise, false.</remarks>
        public Task<bool> IsValidAppIdAsync(string appId)
        {
            return Task.FromResult(this.credentials.ContainsKey(appId));
        }

        /// <summary>
        /// Gets the app password for a given bot app ID.
        /// </summary>
        /// <param name="appId">The ID of the app to get the password for.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful and the app ID is valid, the result
        /// contains the password; otherwise, null.
        /// </remarks>
        public Task<string> GetAppPasswordAsync(string appId)
        {
            return Task.FromResult(this.credentials.ContainsKey(appId) ? this.credentials[appId] : null);
        }

        /// <summary>
        /// Checks whether bot authentication is disabled.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful and bot authentication is disabled, the result
        /// is true; otherwise, false.
        /// </remarks>
        public Task<bool> IsAuthenticationDisabledAsync()
        {
            return Task.FromResult(!this.credentials.Any());
        }
    }
}
