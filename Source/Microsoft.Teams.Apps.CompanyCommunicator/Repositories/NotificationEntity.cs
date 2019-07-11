// <copyright file="NotificationEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Notification entity class used in repository.
    /// This class contains a collection of Recipeint entities.
    /// </summary>
    public class NotificationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets Title value.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets Date value.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification is sent out or not.
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// Gets or sets recipients.
        /// </summary>
        public IEnumerable<RecipientEntity> Recipients { get; set; }
    }
}