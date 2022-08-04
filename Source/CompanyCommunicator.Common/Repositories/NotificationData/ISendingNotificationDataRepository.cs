// <copyright file="ISendingNotificationDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData
{
    using System.Threading.Tasks;

    /// <summary>
    /// interface for Sending Notification Data Repository.
    /// </summary>
    public interface ISendingNotificationDataRepository : IRepository<SendingNotificationDataEntity>
    {
        /// <summary>
        /// Get Adaptive Card from external storage.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <returns>AC in json format.</returns>
        public Task<string> GetAdaptiveCardAsync(string blobName);

        /// <summary>
        /// Save Adaptive Card to external storage.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <param name="adaptiveCard">Adaptive card in json format.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task SaveAdaptiveCardAsync(string blobName, string adaptiveCard);

        /// <summary>
        /// Get image from external storage.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <returns>Image in base64 format.</returns>
        public Task<string> GetImageAsync(string blobName);
    }
}