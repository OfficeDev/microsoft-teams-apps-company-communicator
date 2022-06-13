// <copyright file="UserAppOptions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers.Options
{
    /// <summary>
    /// User app options.
    /// </summary>
    public class UserAppOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether user app should be proactively installed.
        /// </summary>
        public bool ProactivelyInstallUserApp { get; set; }

        /// <summary>
        /// Gets or sets User app's external Id (id in the manifest).
        /// </summary>
        public string UserAppExternalId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the images should be uploaded to Azure Blob Storage.
        /// </summary>
        public bool ImageUploadBlobStorage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how much time the SAS token is valid to access the images uploaded to Azure Blob Storage.
        /// </summary>
        public int ImageUploadBlobStorageSasDurationHours { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether get or sets a value indicating if the tracking is disabled or not.
        /// </summary>
        public bool DisableReadTracking { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of teams you can select to receive a message.
        /// </summary>
        public int MaxNumberOfTeams { get; set; }
    }
}
