﻿// <copyright file="AppConfigurations.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    /// <summary>
    /// Application configuration data model class.
    /// </summary>
    public class AppConfigurations
    {
        /// <summary>
        /// Gets or sets the Microsoft app ID for the bot.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets application TargetingEnabled.
        /// </summary>
        public string TargetingEnabled { get; set; }

        /// <summary>
        /// Gets or sets application MasterAdminUpns.
        /// </summary>
        public string MasterAdminUpns { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets application ImageUploadBlobStorage.
        /// </summary>
        public bool ImageUploadBlobStorage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets a value indicating if the tracking is disabled for CC.
        /// </summary>
        public bool DisableReadTracking { get; set; }
    }
}