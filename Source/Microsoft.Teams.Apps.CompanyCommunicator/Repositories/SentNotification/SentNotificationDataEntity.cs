// <copyright file="SentNotificationDataEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.SentNotification
{
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// Sent notification entity class.
    /// </summary>
    public class SentNotificationDataEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets NotificationId value.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets AudiencesInString value;
        /// This property helps to save the Audiences list in Azure Table storage.
        /// Table Storage doesn't support array type of property directly.
        /// </summary>
        public string AudiencesInString { get; set; }

        /// <summary>
        /// Gets or sets Audiences value.
        /// </summary>
        [IgnoreProperty]
        public IEnumerable<RecipientEntity> Audiences
        {
            get
            {
                return JsonConvert.DeserializeObject<IEnumerable<RecipientEntity>>(this.AudiencesInString);
            }

            set
            {
                this.AudiencesInString = JsonConvert.SerializeObject(value);
            }
        }

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