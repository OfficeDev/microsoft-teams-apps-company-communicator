// <copyright file="NotificationEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// Notification entity class used in repository.
    /// This class contains a collection of Recipeint entities.
    /// </summary>
    public class NotificationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Title value.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the Image Link value.
        /// </summary>
        public string ImageLink { get; set; }

        /// <summary>
        /// Gets or sets the Summary value.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the Author value.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the Button Title value.
        /// </summary>
        public string ButtonTitle { get; set; }

        /// <summary>
        /// Gets or sets the Button Link value.
        /// </summary>
        public string ButtonLink { get; set; }

        /// <summary>
        /// Gets or sets the Created Date value.
        /// </summary>
        public string CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the Sent Date value.
        /// </summary>
        public string SentDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification is sent out or not.
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// Gets or sets TeamsInString value.
        /// This property is a walk around to save the Teams list in Table Storage.
        /// </summary>
        public string TeamsInString { get; set; }

        /// <summary>
        /// Gets or sets Teams audience collection.
        /// </summary>
        [IgnoreProperty]
        public IEnumerable<AudienceEntity> Teams
        {
            get
            {
                return JsonConvert.DeserializeObject<IEnumerable<AudienceEntity>>(this.TeamsInString);
            }

            set
            {
                this.TeamsInString = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// Gets or sets RostersInString value.
        /// This property is a walk around to save the Rosters list in Table Storage.
        /// </summary>
        public string RostersInString { get; set; }

        /// <summary>
        /// Gets or sets Rosters audience collection.
        /// </summary>
        [IgnoreProperty]
        public IEnumerable<AudienceEntity> Rosters
        {
            get
            {
                return JsonConvert.DeserializeObject<IEnumerable<AudienceEntity>>(this.RostersInString);
            }

            set
            {
                this.RostersInString = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a notification should be sent to all the users.
        /// </summary>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Gets or sets the number of audiences received the notification successfully.
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the number of audiences who failed in receiving the notification.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the number of audiences throttled out.
        /// </summary>
        public int Throttled { get; set; }
    }
}