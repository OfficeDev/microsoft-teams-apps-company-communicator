// <copyright file="DraftNotificationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Auth;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Controller for the draft notification data.
    /// </summary>
    [Authorize(PolicyNames.MustHaveUpnClaimPolicy)]
    public class DraftNotificationsController
    {
        /// <summary>
        /// Get draft notifications.
        /// </summary>
        /// <returns>A list of <see cref="Notification"/> instances.</returns>
        [HttpGet("api/draftNotifications")]
        public IEnumerable<Notification> GetDraftNotifications()
        {
            var result = this.GetFakeNotifications();

            return result;
        }

        /// <summary>
        /// Get a draft notification by Id.
        /// </summary>
        /// <param name="id">Draft notification Id.</param>
        /// <returns>Required draft notification.</returns>
        [HttpGet("api/draftNotifications/{id}")]
        public Notification GetDraftNotificationById(int id)
        {
            return
                new Notification
                {
                    Id = id,
                    Title = "A Testing Message (Draft from service)",
                    Date = "12/16/2018",
                    Recipients = "30,0,1",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                };
        }

        private IEnumerable<Notification> GetFakeNotifications()
        {
            var result = new List<Notification>
            {
                new Notification
                {
                    Id = 1,
                    Title = "A Testing Message (Draft from service)",
                    Date = "12/16/2018",
                    Recipients = "30,0,1",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 2,
                    Title = "Testing",
                    Date = "11/16/2019",
                    Recipients = "40,6,8",
                    Acknowledgements = "acknowledgements (Draft from service)",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 3,
                    Title = "Security Advisory Heightened Security During New Year's Eve Celebrations (Draft from service)",
                    Date = "12/16/2019",
                    Recipients = "90,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 4,
                    Title = "Security Advisory Heightened Security During New Year's Eve Celebrations (Draft from service)",
                    Date = "12/16/2019",
                    Recipients = "40,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 5,
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
