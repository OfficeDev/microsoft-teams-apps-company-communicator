// <copyright file="BotOptions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    /// <summary>
    /// Options used for holding meta data for the bot.
    /// </summary>
    public class BotOptions
    {
        /// <summary>
        /// Gets or sets the User app ID for the user bot.
        /// </summary>
        public string UserAppId { get; set; }

        /// <summary>
        /// Gets or sets the User app password for the user bot.
        /// </summary>
        public string UserAppPassword { get; set; }

        /// <summary>
        /// Gets or sets the Author app ID for the author bot.
        /// </summary>
        public string AuthorAppId { get; set; }

        /// <summary>
        /// Gets or sets the Author app password for the author bot.
        /// </summary>
        public string AuthorAppPassword { get; set; }

        /// <summary>
        /// Gets or sets the Graph app ID for the author bot.
        /// </summary>
        public string GraphAppId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use certificates.
        /// </summary>
        public bool UseCertificate { get; set; }

        /// <summary>
        /// Gets or sets the certificate name of the author app.
        /// </summary>
        public string AuthorAppCertName { get; set; }

        /// <summary>
        /// Gets or sets the certificate name of the user app.
        /// </summary>
        public string UserAppCertName { get; set; }

        /// <summary>
        /// Gets or sets the certificate name of the graph app.
        /// </summary>
        public string GraphAppCertName { get; set; }
    }
}
