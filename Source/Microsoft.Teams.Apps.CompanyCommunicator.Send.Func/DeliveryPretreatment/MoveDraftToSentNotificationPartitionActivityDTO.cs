// <copyright file="MoveDraftToSentNotificationPartitionActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the MoveDraftToSentNotificationPartitionActivity as parameter type.
    /// </summary>
    public class MoveDraftToSentNotificationPartitionActivityDTO
    {
        /// <summary>
        /// Gets or sets total audience count.
        /// </summary>
        public int TotalAudienceCount { get; set; }

        /// <summary>
        /// Gets or sets draft notification entity.
        /// </summary>
        public NotificationDataEntity DraftNotificationEntity { get; set; }
    }
}