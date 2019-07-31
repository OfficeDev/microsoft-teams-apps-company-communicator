// <copyright file="TableRowKeyGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories
{
    using System;

    /// <summary>
    /// This class uses the log tail pattern to generate row keys.
    /// The generated rowKeys are based off timestamps, so that the order in the table is from most recent to least recent.
    /// </summary>
    public class TableRowKeyGenerator
    {
        /// <summary>
        /// Generate a new row key using log tail pattern. Most recent => oldest.
        /// </summary>
        /// <returns>A new row key.</returns>
        public string NewKeyInLogTailPattern()
        {
            var invertedTicksInString = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);

            return invertedTicksInString;
        }

        /// <summary>
        /// Generate a new row key using log head pattern. Oldest => most recent.
        /// </summary>
        /// <returns>A new row key.</returns>
        public string NewKeyInLogHeadPattern()
        {
            var invertedTicksInString = string.Format("{0:D19}", DateTime.UtcNow.Ticks);

            return invertedTicksInString;
        }
    }
}
