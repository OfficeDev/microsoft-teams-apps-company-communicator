// <copyright file="SendTriggerToDataFunctionActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    /// <summary>
    /// DTO class used by the durable framework to pass parameter to SendTriggerToDataFunctionActivity.
    /// </summary>
    public class SendTriggerToDataFunctionActivityDTO
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