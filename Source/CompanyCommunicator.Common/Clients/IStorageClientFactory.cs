// <copyright file="IStorageClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Clients
{
    using global::Azure.Storage.Blobs;

    /// <summary>
    /// Storage client factory.
    /// </summary>
    public interface IStorageClientFactory
    {
        /// <summary>
        /// Create the blob container client instance.
        /// </summary>
        /// <returns>BlobContainerClient instance.</returns>
        BlobContainerClient CreateBlobContainerClient();

        /// <summary>
        /// Create the blob container client instance.
        /// </summary>
        /// <param name="blobContainerName">Blob container name.</param>
        /// <returns>BlobContainerClient instance.</returns>
        BlobContainerClient CreateBlobContainerClient(string blobContainerName);
    }
}
