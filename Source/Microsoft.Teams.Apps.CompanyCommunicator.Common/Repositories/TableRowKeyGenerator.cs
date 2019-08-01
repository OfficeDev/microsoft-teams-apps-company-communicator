// <copyright file="TableRowKeyGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    using System;

    /// <summary>
    /// This class generates rowKeys based off timestamps so that the order of the table is correct.
    /// </summary>
    public class TableRowKeyGenerator
    {
        /// <summary>
        /// Generate a new row key based off of the current timestamp such that the keys are ordered most recent => oldest.
        /// </summary>
        /// <returns>A new row key.</returns>
        public string NewKeyOrderingMostRecentToOldest()
        {
            var invertedTickString = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);

            return invertedTickString;
        }

        /// <summary>
        /// Generate a new row key based off of the current timestamp such that the keys are ordered oldest => most recent.
        /// </summary>
        /// <returns>A new row key.</returns>
        public string NewKeyOrderingOldestToMostRecent()
        {
            var ticksString = string.Format("{0:D19}", DateTime.UtcNow.Ticks);

            return ticksString;
        }
    }
}
