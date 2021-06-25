// <copyright file="UserExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;

    /// <summary>
    /// Extensions for User Ids.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// this is as per microsoft graph api filter size.
        /// </summary>
        private static readonly int MaxGroupSize = 15;

        /// <summary>
        /// Break the list in groups.
        /// </summary>
        /// <param name="userIds">the user ids.</param>
        /// <returns>group list of user id list.</returns>
        public static IEnumerable<IEnumerable<string>> AsGroups(this IList<string> userIds)
        {
            var buffer = new List<string>(MaxGroupSize);
            for (int i = 0; i < userIds.Count; i++)
            {
                buffer.Add(userIds[i]);
                if (((i + 1) % MaxGroupSize) == 0 && buffer.Count > 0)
                {
                    yield return buffer;
                    buffer = new List<string>(MaxGroupSize);
                }
            }

            if (buffer.Count > 0)
            {
                yield return buffer;
            }
        }

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
