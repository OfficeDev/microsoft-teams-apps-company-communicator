// <copyright file="IDataStreamFacade.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;

    /// <summary>
    /// Facade to get the data stream.
    /// </summary>
    public interface IDataStreamFacade
    {
        /// <summary>
        /// Get the user data list, which can be iterated asynchronously.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <param name="notificationStatus">the notification status.</param>
        /// <returns>the streams of user data.</returns>
        IAsyncEnumerable<IEnumerable<UserData>> GetUserDataStreamAsync(string notificationId, string notificationStatus);

        /// <summary>
        /// Get the team data list, which can be iterated asynchronously.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <param name="notificationStatus">the notification status.</param>
        /// <returns>the streams of team data.</returns>
        IAsyncEnumerable<IEnumerable<TeamData>> GetTeamDataStreamAsync(string notificationId, string notificationStatus);
    }
}
