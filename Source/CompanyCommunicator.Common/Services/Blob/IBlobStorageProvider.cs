// <copyright file="IBlobStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Blob
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for handling Azure Blob Storage operations like uploading and downloading images/Adaptive Cards from blob.
    /// </summary>
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// Upload Adaptive Card to blob container.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <param name="adaptiveCard">Adaptive card in json format.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task UploadAdaptiveCardAsync(string blobName, string adaptiveCard);

        /// <summary>
        /// Download Adaptive Card from blob container.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <returns>Adaptive card in json format.</returns>
        public Task<string> DownloadAdaptiveCardAsync(string blobName);

        /// <summary>
        /// Upload an image to blob container.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <param name="base64Image">Image in base64 format without prefix.</param>
        /// <returns>Prefix with mime, ex: data:image/png;base64,.</returns>
        public Task<string> UploadBase64ImageAsync(string blobName, string base64Image);

        /// <summary>
        /// Download an image from blob container.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <returns>Image in base64 format without prefix.</returns>
        public Task<string> DownloadBase64ImageAsync(string blobName);

        /// <summary>
        /// Delete a blob and all of its snapshots.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task DeleteImageBlobAsync(string blobName);

        /// <summary>
        /// Copy a source blob to a destination blob with a different name.
        /// </summary>
        /// <param name="blobName">Source blob name.</param>
        /// <param name="newBlobName">Destination blob name.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task CopyImageBlobAsync(string blobName, string newBlobName);
    }
}