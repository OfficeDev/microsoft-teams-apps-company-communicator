// <copyright file="RecipientEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Recipeint entity class used in the respository server.
    /// A notification entity has a collection of Recipeint entities.
    /// </summary>
    public class RecipientEntity
    {
        /// <summary>
        /// Gets or sets recipient Id.
        /// </summary>
        public string RecipientId { get; set; }

        /// <summary>
        /// Gets or sets notification state.
        /// </summary>
        public DeliveryState NotificationState { get; set; }

        // other properties
        // Acknowlegement
        // Recation
        // Response
    }
}
