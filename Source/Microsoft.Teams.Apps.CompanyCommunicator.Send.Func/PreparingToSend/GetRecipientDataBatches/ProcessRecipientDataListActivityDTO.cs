// <copyright file="ProcessRecipientDataListActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the GetRecipientDataBatchesActivity as parameter type.
    /// </summary>
    public class ProcessRecipientDataListActivityDTO
    {
        /// <summary>
        /// Gets or sets recipient data batches.
        /// </summary>
        public IEnumerable<UserDataEntity> RecipientDataList { get; set; }

        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }
    }
}