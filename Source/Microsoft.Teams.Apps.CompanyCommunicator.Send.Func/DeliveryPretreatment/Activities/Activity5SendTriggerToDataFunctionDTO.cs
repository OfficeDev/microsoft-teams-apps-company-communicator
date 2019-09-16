// <copyright file="Activity5SendTriggerToDataFunctionDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    /// <summary>
    /// DTO class used by the SendTriggerToDataFunctionActivity as parameter type.
    /// </summary>
    public class Activity5SendTriggerToDataFunctionDTO
    {
        /// <summary>
        /// Gets or sets notification id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets total message count.
        /// </summary>
        public int TotalMessagesToBeSentToServiceBusCount { get; set; }
    }
}