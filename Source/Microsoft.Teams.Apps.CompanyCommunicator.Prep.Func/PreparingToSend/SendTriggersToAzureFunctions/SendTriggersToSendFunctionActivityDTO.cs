// <copyright file="SendTriggersToSendFunctionActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;

    /// <summary>
    /// DTO class used by the durable framework to pass parameters to the SendTriggersToSendFunctionActivity.
    /// </summary>
    public class SendTriggersToSendFunctionActivityDTO
    {
        /// <summary>
        /// Gets or sets the notification Id.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the recipient data batch index.
        /// </summary>
        public int RecipientDataBatchIndex { get; set; }
    }
}
