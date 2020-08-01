// <copyright file="GetRecipientDataListForRosterActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;

    /// <summary>
    /// DTO class used by the durable framework to pass parameter to the GetRecipientDataListForRosterActivity.
    /// </summary>
    public class GetRecipientDataListForRosterActivityDTO
    {
        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }

        /// <summary>
        /// Gets or sets team data entity.
        /// </summary>
        public TeamDataEntity TeamDataEntity { get; set; }
    }
}