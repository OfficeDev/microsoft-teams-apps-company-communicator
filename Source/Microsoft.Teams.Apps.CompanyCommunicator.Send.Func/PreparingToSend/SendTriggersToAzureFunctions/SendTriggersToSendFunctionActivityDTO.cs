// <copyright file="SendTriggersToSendFunctionActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the duralbe framework to pass parameter to the SendTriggerToDataFunctionActivity.
    /// </summary>
    public class SendTriggersToSendFunctionActivityDTO
    {
        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }

        /// <summary>
        /// Gets or sets recipient data batch.
        /// </summary>
        public IEnumerable<UserDataEntity> RecipientDataBatch { get; set; }

        /// <summary>
        /// Gets or sets recipient status dictionary.
        /// </summary>
        public IDictionary<string, int> RecipientStatusDictionary { get; set; }
    }
}