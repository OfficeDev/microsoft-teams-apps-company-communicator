// <copyright file="UserExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;

    /// <summary>
    /// Extensions for User Ids.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// Get the userType for a user.
        /// </summary>
        /// <param name="userPrincipalName">the user principal name.</param>
        /// <returns>the user type such as Member or Guest.</returns>
        public static string GetUserType(this string userPrincipalName)
        {
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new ArgumentNullException(nameof(userPrincipalName));
            }

            return userPrincipalName.ToLower().Contains("#ext#") ? UserType.Guest : UserType.Member;
        }

        /// <summary>
        /// Get the userType for a user.
        /// </summary>
        /// <param name="user">the microsoft graph user.</param>
        /// <returns>the user type such as Member or Guest.</returns>
        public static string GetUserType(this User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!string.IsNullOrEmpty(user.UserType))
            {
                return user.UserType;
            }

            return user.UserPrincipalName.GetUserType();
        }
    }
}
