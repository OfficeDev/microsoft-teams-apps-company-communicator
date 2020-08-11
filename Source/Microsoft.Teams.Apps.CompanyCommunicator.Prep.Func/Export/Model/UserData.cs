// <copyright file="UserData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model
{
    using CsvHelper.Configuration.Attributes;

    /// <summary>
    /// the model class for user data.
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Name("User ID")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user principal name.
        /// </summary>
        [Name("UPN")]
        public string Upn { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [Name("Name")]
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