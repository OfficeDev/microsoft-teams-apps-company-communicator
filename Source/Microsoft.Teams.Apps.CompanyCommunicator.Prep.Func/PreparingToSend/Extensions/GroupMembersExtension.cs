// <copyright file="GroupMembersExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Extension methods for the Graph User.
    /// </summary>
    public static class GroupMembersExtension
    {
        /// <summary>
        /// Filter the installed users from group members on Aad Id.
        /// </summary>
        /// <param name="groupMembers"> group members.</param>
        /// <param name="installedUsers">installed app users.</param>
        /// <returns> filtered group members.</returns>
        public static IEnumerable<User> FilterInstalledUsers(
             this IEnumerable<User> groupMembers,
             IEnumerable<UserDataEntity> installedUsers)
        {
            var installedUserIdSet = installedUsers.Select(user => user.AadId).ToHashSet();
            return groupMembers.
                Where(member => !installedUserIdSet.Contains(member.Id));
        }

        /// <summary>
        /// converts group member in user data entity.
        /// </summary>
        /// <param name="groupMembers">group members.</param>
        /// <returns>list of user data entity.</returns>
        public static IEnumerable<UserDataEntity> Convert(
                        this IEnumerable<User> groupMembers)
        {
            var remainingUserEntities = new List<UserDataEntity>();
            foreach (var remUser in groupMembers)
            {
                remainingUserEntities.Add(new UserDataEntity()
                {
                    AadId = remUser.Id,
                    Name = remUser.DisplayName,
                    Email = remUser.Mail,
                    Upn = remUser.UserPrincipalName,
                });
            }

            return remainingUserEntities;
        }

        /// <summary>
        /// extracts the next page url.
        /// </summary>
        /// <param name="additionalData">dictionary contaning odata next page link.</param>
        /// <returns>next page url.</returns>
        public static string NextPageUrl(this IDictionary<string, object> additionalData)
        {
            additionalData.TryGetValue(Common.Constants.ODataNextPageLink, out object nextLink);
            var nextPageUrl = (nextLink == null) ? string.Empty : nextLink.ToString();
            return nextPageUrl;
        }
    }
}
