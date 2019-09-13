// <copyright file="SendTriggersToSendFunctionActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the SendTriggersToSendFunctionActivity as parameter type.
    /// </summary>
    public class SendTriggersToSendFunctionActivityDTO
    {
        /// <summary>
        /// Gets or sets notification id.
        /// </summary>
        public string NewSentNotificationId { get; set; }

        /// <summary>
        /// Gets or sets total message count.
        /// </summary>
        public IList<UserDataEntity> DeduplicatedReceiverEntities { get; set; }
    }
}