namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers.Options;

    /// <summary>
    /// Controller for ptions.
    /// </summary>
    [Route("api/options")]
    public class OptionsController : ControllerBase
    {
        private readonly UserAppOptions userAppOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsController"/> class.
        /// </summary>
        /// <param name="userAppOptions">the user app options.</param>
        public OptionsController(IOptions<UserAppOptions> userAppOptions)
        {
            this.userAppOptions = userAppOptions?.Value ?? throw new ArgumentNullException(nameof(userAppOptions));
        }

        /// <summary>
        /// Returns the maximum number of teams that can receive a message.
        /// </summary>
        /// <returns>The maximun number of teams that can receive a message.</returns>
        [HttpGet]
        public ActionResult<string> GetMaxNumberOfTeams()
        {
            return this.Ok(this.userAppOptions.MaxNumberOfTeams);
        }
    }
}
