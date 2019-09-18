// <copyright file="SendTriggersToAzureFunctionsOrchestrationDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// DTO class used by the SendTriggerToAzureFunctionsOrchestration as parameter type.
    /// </summary>
    public class SendTriggersToAzureFunctionsOrchestrationDTO
    {
        /// <summary>
        /// Gets or sets notification data entity.
        /// </summary>
        public NotificationDataEntity NotificationDataEntity { get; set; }

        /// <summary>
        /// Gets or sets recipient data batch.
        /// </summary>
        public IEnumerable<IEnumerable<UserDataEntity>> RecipientDataBatches { get; set; }
    }
}