// <copyright file="EnumerableExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Group Extension.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Check if the list is null or empty.
        /// </summary>
        /// <typeparam name="T">entity class type.</typeparam>
        /// <param name="enumerable">the list of types.</param>
        /// <returns>Indicating if the list is empty or null.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable?.Any() ?? true;
        }
    }
}
