// <copyright file="UserExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Extensions
{
    using System.Collections.Generic;

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
    }
}
