// <copyright file="Activity4SendTriggersToSendFunctionDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the SendTriggerToDataFunctionActivity as parameter type.
    /// </summary>
    public class Activity4SendTriggersToSendFunctionDTO
    {
        /// <summary>
        /// Gets or sets new sent notification id.
        /// </summary>
        public string NewSentNotificationId { get; set; }

        /// <summary>
        /// Gets or sets .
        /// </summary>
        public List<UserDataEntity> ReceiverBatch { get; set; }
    }
}