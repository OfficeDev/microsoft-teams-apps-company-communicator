// <copyright file="NotificationDeliveryStatusDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services
{
    using System;

    /// <summary>
    /// Notification delivery status DTO class.
    /// </summary>
    public class NotificationDeliveryStatusDTO
    {
        private DateTime lastSentDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDeliveryStatusDTO"/> class.
        /// </summary>
        public NotificationDeliveryStatusDTO()
        {
            this.Succeeded = 0;
            this.Throttled = 0;
            this.Failed = 0;
            this.Unknown = 0;
            this.LastSentDate = DateTime.MinValue;
        }

        /// <summary>
        /// Gets or sets succeeded recipient count.
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Gets or sets failed recipient count.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets throttled recipient count.
        /// </summary>
        public int Throttled { get; set; }

        /// <summary>
        /// Gets or sets unknown status recipient count.
        /// </summary>
        public int Unknown { get; set; }

        /// <summary>
        /// Gets current message count.
        /// Purposefully exclude the unknown count because those messages may be sent later.
        /// </summary>
        public int CurrentMessageCount
        {
            get
            {
                return this.Succeeded + this.Failed + this.Throttled;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the notification's delivery is complete.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets last sent date time.
        /// </summary>
        /// <returns>It returns the last sent date time.</returns>
        public DateTime LastSentDate
        {
            get
            {
                return this.lastSentDate;
            }

            set
            {
                this.lastSentDate = value > this.lastSentDate ? value : this.lastSentDate;
            }
        }

        /// <summary>
        /// Set last sent date time if it's not set yet.
        /// </summary>
        public void SetLastSentDateIfNotSetYet()
        {
            if (this.lastSentDate == DateTime.MinValue)
            {
                this.lastSentDate = DateTime.UtcNow;
            }
        }
    }
}