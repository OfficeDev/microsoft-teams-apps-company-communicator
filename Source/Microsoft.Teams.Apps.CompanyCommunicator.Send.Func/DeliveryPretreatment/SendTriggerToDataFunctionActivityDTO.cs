// <copyright file="SendTriggerToDataFunctionActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    /// <summary>
    /// DTO class used by the SendTriggerToDataFunctionActivity as parameter type.
    /// </summary>
    public class SendTriggerToDataFunctionActivityDTO
    {
        /// <summary>
        /// Gets or sets notification id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets total message count.
        /// </summary>
        public int TotalMessageCount { get; set; }
    }
}