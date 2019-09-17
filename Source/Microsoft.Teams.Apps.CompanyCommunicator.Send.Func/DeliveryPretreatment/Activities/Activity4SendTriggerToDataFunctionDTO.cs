// <copyright file="Activity4SendTriggerToDataFunctionDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    /// <summary>
    /// DTO class used by the SendTriggerToDataFunctionActivity as parameter type.
    /// </summary>
    public class Activity4SendTriggerToDataFunctionDTO
    {
        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }

        /// <summary>
        /// Gets or sets total recipient count.
        /// </summary>
        public int TotalRecipientCount { get; set; }
    }
}