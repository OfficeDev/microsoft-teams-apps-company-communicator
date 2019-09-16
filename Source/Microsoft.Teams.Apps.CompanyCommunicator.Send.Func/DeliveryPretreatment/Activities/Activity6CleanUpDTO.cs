// <copyright file="Activity6CleanUpDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// DTO class used by the Activity7CleanUp as parameter type.
    /// </summary>
    public class Activity6CleanUpDTO
    {
        /// <summary>
        /// Gets or sets new sent notification id.
        /// </summary>
        public string NewSentNotificationId { get; set; }

        /// <summary>
        /// Gets or sets draft notification entity.
        /// </summary>
        public NotificationDataEntity DraftNotificationEntity { get; set; }

        /// <summary>
        /// Gets or sets Exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}