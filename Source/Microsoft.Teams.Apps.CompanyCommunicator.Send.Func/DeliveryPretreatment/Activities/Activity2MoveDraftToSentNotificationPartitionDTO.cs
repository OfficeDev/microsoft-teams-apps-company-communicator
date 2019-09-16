// <copyright file="Activity2MoveDraftToSentNotificationPartitionDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// DTO class used by the MoveDraftToSentNotificationPartitionActivity as parameter type.
    /// </summary>
    public class Activity2MoveDraftToSentNotificationPartitionDTO
    {
        /// <summary>
        /// Gets or sets total audience count.
        /// </summary>
        public int TotalMessagesToBeSentToServiceBusCount { get; set; }

        /// <summary>
        /// Gets or sets draft notification entity.
        /// </summary>
        public NotificationDataEntity DraftNotificationEntity { get; set; }
    }
}