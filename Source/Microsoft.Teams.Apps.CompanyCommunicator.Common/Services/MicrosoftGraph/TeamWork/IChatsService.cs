// <copyright file="IChatsService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System.Threading.Tasks;

    /// <summary>
    /// Chats Service.
    /// </summary>
    public interface IChatsService
    {
        /// <summary>
        /// Get chatThread Id for the user.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="appId">Teams App Id.</param>
        /// <returns>ChatThread Id.</returns>
        public Task<string> GetChatThreadIdAsync(string userId, string appId);
    }
}
