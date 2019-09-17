// <copyright file="Activity1GetRecipientDataBatchesDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the SendTriggerToPretreatFunctionActivity as parameter type.
    /// </summary>
    public class Activity1GetRecipientDataBatchesDTO
    {
        /// <summary>
        /// Gets or sets recipient data batches.
        /// </summary>
        public List<List<UserDataEntity>> RecipientDataBatches { get; set; }

        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }
    }
}