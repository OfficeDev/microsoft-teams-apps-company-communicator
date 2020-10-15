// <copyright file="HealthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Controller for health endpoint.
    /// </summary>
    [Route("[controller]")]
    public class HealthController : Controller
    {
        /// <summary>
        /// Report health status of the application.
        /// </summary>
        /// <returns>Action.</returns>
        [HttpGet]
        public ActionResult Index()
        {
            return this.Ok();
        }
    }
}
