// <copyright file="IUserTypeService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// The user type service interface.
    /// </summary>
    public interface IUserTypeService
    {
        /// <summary>
        /// Update user type of existing user if it is not set.
        /// </summary>
        /// <param name="userDataEntity">User Data Entity.</param>
        /// <param name="userType">Type of the user such as Member or Guest.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task UpdateUserTypeForExistingUserAsync(UserDataEntity userDataEntity, string userType);

        /// <summary>
        /// Update user type of existing user list if it is not set.
        /// </summary>
        /// <param name="userDataEntities">User Data Entity list.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task UpdateUserTypeForExistingUserListAsync(IEnumerable<UserDataEntity> userDataEntities);
    }
}
