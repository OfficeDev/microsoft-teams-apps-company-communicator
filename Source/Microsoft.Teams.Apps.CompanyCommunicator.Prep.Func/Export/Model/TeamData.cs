// <copyright file="TeamData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model
{
    using CsvHelper.Configuration.Attributes;

    /// <summary>
    /// the model class for team data.
    /// </summary>
    public class TeamData
    {
        /// <summary>
        /// Gets or sets the team id value.
        /// </summary>
        [Name("Team ID")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the team id value.
        /// </summary>
        [Name("Team Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the delivery status value.
        /// </summary>
        [Name("Delivery Status")]
        public string DeliveryStatus { get; set; }

        /// <summary>
        /// Gets or sets the status reason value.
        /// </summary>
        [Name("Status Reason")]
        public string StatusReason { get; set; }
    }
}
