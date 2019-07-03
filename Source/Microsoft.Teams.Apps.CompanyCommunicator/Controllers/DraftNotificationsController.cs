// <copyright file="DraftNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Controller for the message data.
    /// </summary>
    public class DraftNotificationsController
    {
        /// <summary>
        /// Get draft notifications.
        /// </summary>
        /// <returns>A list of <see cref="Notification"/> instances.</returns>
        [HttpGet("api/draftNotifications")]
        public IEnumerable<Notification> GetDraftMessages()
        {
            var result = this.GetFakeMessages();

            return result;
        }

        private IEnumerable<Notification> GetFakeMessages()
        {
            var result = new List<Notification>
            {
                new Notification
                {
                    Title = "A Testing Message (Draft from service)",
                    Date = "12/16/2018",
                    Recipients = "30,0,1",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Title = "Testing",
                    Date = "11/16/2019",
                    Recipients = "40,6,8",
                    Acknowledgements = "acknowledgements (Draft from service)",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Title = "Security Advisory Heightened Security During New Year's Eve Celebrations (Draft from service)",
                    Date = "12/16/2019",
                    Recipients = "90,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Title = "Security Advisory Heightened Security During New Year's Eve Celebrations (Draft from service)",
                    Date = "12/16/2019",
                    Recipients = "40,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Title = "Upcoming Holiday (Draft from service)",
                    Date = "12/16/2019",
                    Recipients = "14,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
            };

            return result;
        }
    }
}
