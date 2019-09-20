// <copyright file="RecipientDataEntityHashSet.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Recipient data entity hash set.
    /// This class helps to get rid of duplicte records in a recipient data list.
    /// </summary>
    public class RecipientDataEntityHashSet : HashSet<UserDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientDataEntityHashSet"/> class.
        /// </summary>
        /// <param name="userDataEntityList">User data entity list.</param>
        public RecipientDataEntityHashSet(IEnumerable<UserDataEntity> userDataEntityList)
            : base(userDataEntityList, new EqualityComarer())
        {
        }

        private class EqualityComarer : IEqualityComparer<UserDataEntity>
        {
            public bool Equals(UserDataEntity x, UserDataEntity y)
            {
                return x.AadId == y.AadId;
            }

            public int GetHashCode(UserDataEntity userDataEntity)
            {
                return userDataEntity.AadId.GetHashCode();
            }
        }
    }
}