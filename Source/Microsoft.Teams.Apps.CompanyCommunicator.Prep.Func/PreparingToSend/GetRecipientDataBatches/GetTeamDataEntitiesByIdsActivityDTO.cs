// <copyright file="GetTeamDataEntitiesByIdsActivityDTO.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;

    /// <summary>
    ///  DTO class used by the durable framework to pass parameter to the GetTeamDataEntitiesByIdsActivity.
    /// </summary>
    public class GetTeamDataEntitiesByIdsActivityDTO
    {
        /// <summary>
        /// Gets or sets notification data entity id.
        /// </summary>
        public string NotificationDataEntityId { get; set; }

        /// <summary>
        /// Gets or sets all team id list.
        /// </summary>
        public IEnumerable<string> TeamIds { get; set; }
    }
}