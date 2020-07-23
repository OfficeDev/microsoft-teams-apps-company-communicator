// <copyright file="UserExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Graph;

    /// <summary>
    /// Extenions for User Ids.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// this is as per microsoft graph api filter size.
        /// </summary>
        private static readonly int MaxGroupSize = 50;

        /// <summary>
        /// Break the list in groups.
        /// </summary>
        /// <param name="userIds">the user ids.</param>
        /// <returns>group list of user id list.</returns>
        public static IEnumerable<List<string>> AsGroups(this List<string> userIds)
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

            yield return buffer;
        }
    }
}
