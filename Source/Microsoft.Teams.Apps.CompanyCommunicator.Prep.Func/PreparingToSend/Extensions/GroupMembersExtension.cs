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
        /// Remove the installed users from group members on Aad Id.
        /// </summary>
        /// <param name="groupMembers"> group members.</param>
        /// <param name="installedUsers">installed app users.</param>
        /// <returns> filtered group members.</returns>
        public static IEnumerable<User> Intersect(
             this IEnumerable<User> groupMembers,
             IEnumerable<UserDataEntity> installedUsers)
        {
            return groupMembers.
                Where(x => !installedUsers.Select(y => y.AadId).
                Contains(x.Id));
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
    }
}