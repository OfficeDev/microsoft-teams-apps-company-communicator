// <copyright file="SentNotificationsController.cs" company="Microsoft">
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
    public class SentNotificationsController
    {
        /// <summary>
        /// Fetch published messages.
        /// </summary>
        /// <returns>A list of <see cref="Notification"/> instances.</returns>
        [HttpGet("api/sentNotifications")]
        public IEnumerable<Notification> GetSentNotifications()
        {
            var result = this.GetFakeNotifications();

            return result;
        }

        /// <summary>
        /// Get a sent notification by Id.
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/sentNotifications/{id}")]
        public Notification GetSentNotificationById(int id)
        {
            return
                new Notification
                {
                    Id = id,
                    Title = "A Testing Message (from service)",
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
                    Id = 6,
                    Title = "A Testing Message (from service)",
                    Date = "12/16/2018",
                    Recipients = "30,0,1",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 7,
                    Title = "Testing",
                    Date = "11/16/2019",
                    Recipients = "40,6,8",
                    Acknowledgements = "acknowledgements (from service)",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 8,
                    Title = "Security Advisory Heightened Security During New Year's Eve Celebrations (from service)",
                    Date = "12/16/2019",
                    Recipients = "90,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 9,
                    Title = "Security Advisory Heightened Security During New Year's Eve Celebrations (from service)",
                    Date = "12/16/2019",
                    Recipients = "40,6,8",
                    Acknowledgements = "acknowledgements",
                    Reactions = "like 3",
                    Responses = "view 3",
                },
                new Notification
                {
                    Id = 10,
                    Title = "Upcoming Holiday (from service)",
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
