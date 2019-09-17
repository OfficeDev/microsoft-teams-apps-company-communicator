// <copyright file="Activity3SendTriggersToSendFunctionDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the SendTriggerToDataFunctionActivity as parameter type.
    /// </summary>
    public class Activity3SendTriggersToSendFunctionDTO
    {
        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }

        /// <summary>
        /// Gets or sets recipient data batch.
        /// </summary>
        public List<UserDataEntity> RecipientDataBatch { get; set; }

        /// <summary>
        /// Gets or sets recipient status dictionary.
        /// </summary>
        public IDictionary<string, int> RecipientStatusDictionary { get; set; }
    }
}