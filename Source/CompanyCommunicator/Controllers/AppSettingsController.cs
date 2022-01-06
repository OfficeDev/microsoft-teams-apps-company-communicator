// <copyright file="AppSettingsController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.DIConnect.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Controller to get app settings.
    /// </summary>
    [Route("api/settings")]
    [ApiController]
    public class AppSettingsController : ControllerBase
    {
        private readonly BotOptions botOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsController"/> class.
        /// </summary>
        /// <param name="userAppOptions">User app options.</param>
        public AppSettingsController(
            IOptions<BotOptions> userAppOptions)
        {
            this.botOptions = userAppOptions?.Value ?? throw new ArgumentNullException(nameof(userAppOptions));
        }

        /// <summary>
        /// Get app id and if targeting is enabled.
        /// </summary>
        /// <returns>Required sent notification.</returns>
        [HttpGet]
        public IActionResult GetAppSettings()
        {
            var appId = this.botOptions.AuthorAppId;
            var targetingEnabled = this.botOptions.TargetingEnabled;
            var masterAdminUpns = this.botOptions.MasterAdminUpns;
            var response = new AppConfigurations()
            {
                AppId = appId,
                TargetingEnabled = targetingEnabled,
                MasterAdminUpns = masterAdminUpns,
            };

            return this.Ok(response);
        }
    }
}