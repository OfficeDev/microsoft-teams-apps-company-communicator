// <copyright file="RecipientEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories
{
    /// <summary>
    /// Recipeint entity class used in respository.
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
        public DeliveryStatus NotificationState { get; set; }

        // other properties
        // Acknowlegement
        // Recation
        // Response
    }
}