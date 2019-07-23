// <copyright file="RecipientEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotification
{
    /// <summary>
    /// Audience entity class.
    /// </summary>
    public class RecipientEntity
    {
        /// <summary>
        /// Gets or sets Aad Id.
        /// </summary>
        public string AadId { get; set; }

        /// <summary>
        /// Gets or sets delivery state.
        /// </summary>
        public DeliveryStatus DeliveryStatus { get; set; }
    }
}