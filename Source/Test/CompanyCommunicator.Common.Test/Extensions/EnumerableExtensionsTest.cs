// <copyright file="EnumerableExtensionsTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Xunit;

    /// <summary>
    /// Enumerable Extensions Test.
    /// </summary>
    public class EnumerableExtensionsTest
    {
        /// <summary>
        /// Gets data for AsBatches Test.
        /// Format: { each batch size, expected count of batches, list of data }.
        /// </summary>
        public static IEnumerable<object[]> Data => new List<object[]>
        {
            // { batch size, expected batch count, input data list }.
            new object[] { 15, 1, GetData(1) },
            new object[] { 15, 2, GetData(20) },
            new object[] { 15, 7, GetData(100) },
            new object[] { 15, 1000, GetData(15000) },
            new object[] { 1000, 100, GetData(100000) },
            new object[] { 1000, 2000, GetData(2000000) },
            new object[] { 1000, 20000, GetData(20000000) },
            new object[] { 1000, 20001, GetData(20000900) },
        };

        /// <summary>
        /// Test case to check if list with value is success.
        /// </summary>
        [Fact]
        public void Check_AllValues_ShouldBeSuccess()
        {
            // Arrange
            var sourceList = new List<string>();
            sourceList.Add("a");

            // Act
            Assert.False(sourceList.IsNullOrEmpty());
        }

        /// <summary>
        /// Test case to check if null list is success.
        /// </summary>
        [Fact]
        public void Check_NullValues_ShouldBeSucess()
        {
            // Arrange
            var sourceEmptyList = new List<string>();
            var sourceNullList = new List<string>();
            sourceNullList = null;

            // Act
            Assert.True(sourceEmptyList.IsNullOrEmpty());
            Assert.True(sourceNullList.IsNullOrEmpty());
        }

        /// <summary>
        /// Test case to check AsBatches is success with multiple values.
        /// </summary>
        /// <param name="batchSize">batch size.</param>
        /// <param name="expectedSize">expected output count.</param>
        /// <param name="inputList">source collection.</param>
        [Theory]
        [MemberData(nameof(Data))]
        public void CheckAsBatches_AllValues_ShouldBeSuccess(int batchSize, int expectedSize, List<string> inputList)
        {
            var result = inputList.AsBatches(batchSize);

            var actualLastListCount = inputList.Count % batchSize;
            Assert.Equal(expectedSize, result.Count());
            Assert.Equal(actualLastListCount == 0 ? batchSize : actualLastListCount, result.Last().Count());
        }

        /// <summary>
        /// Get the list of data.
        /// </summary>
        /// <param name="count">the count.</param>
        /// <returns>list of data.</returns>
        private static List<string> GetData(int count)
        {
            var collection = new List<string>();

            for (int i = 1; i <= count; i++)
            {
                collection.Add($"user_{i}");
            }

            return collection;
        }
    }
}
