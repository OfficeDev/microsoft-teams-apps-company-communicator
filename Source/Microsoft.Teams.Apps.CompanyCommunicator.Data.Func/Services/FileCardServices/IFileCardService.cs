// <copyright file="IFileCardService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.FileCardServices
{
    using System.Threading.Tasks;

    /// <summary>
    /// The file card service to manange the card.
    /// </summary>
    public interface IFileCardService
    {
        /// <summary>
        /// Delete the card and send the permission expired message.
        /// </summary>
        /// <param name="userId">the user id.</param>
        /// <param name="fileConsentId">the file consent id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteAsync(string userId, string fileConsentId);
    }
}
