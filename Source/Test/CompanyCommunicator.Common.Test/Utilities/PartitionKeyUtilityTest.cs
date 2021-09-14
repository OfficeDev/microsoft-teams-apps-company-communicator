// <copyright file="PartitionKeyUtilityTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Utilities
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Utilities;
    using Xunit;

    /// <summary>
    /// PartitionKeyUtility Test.
    /// </summary>
    public class PartitionKeyUtilityTest
    {
        /// <summary>
        /// Test case to check if batch partition key is generated as expected.
        /// </summary>
        [Fact]
        public void CreateBatchPartitionKey_CorrectParameters_ShouldBeSuccess()
        {
            // Arrange
            string notificationId = "notificationId";
            int batchIndex = 1;
            var expectedResult = $"{notificationId}:{batchIndex}";

            // Act
            var result = PartitionKeyUtility.CreateBatchPartitionKey(notificationId, batchIndex);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test case to check if notification id can be extracted from batch partition key.
        /// </summary>
        [Fact]
        public void GetNotificationIdFromBatchPartitionKey_CorrectParameters_ShouldBeSuccess()
        {
            // Arrange
            string batchPartitionKey = "notificationId:1";
            var expectedResult = "notificationId";

            // Act
            var result = PartitionKeyUtility.GetNotificationIdFromBatchPartitionKey(batchPartitionKey);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test case to check if batch id can be extracted from batch partition key.
        /// </summary>
        [Fact]
        public void GetBatchIdFromBatchPartitionKey_CorrectParameters_ShouldBeSuccess()
        {
            // Arrange
            string batchPartitionKey = "notificationId:1";
            var expectedResult = "1";

            // Act
            var result = PartitionKeyUtility.GetBatchIdFromBatchPartitionKey(batchPartitionKey);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test case to check if exception is thrown if batch partition key is of not expected format.
        /// </summary>
        [Fact]
        public void GetNotificationIdFromBatchPartitionKey_InCorrectParameters_ShouldBeSuccess()
        {
            // Arrange
            string batchPartitionKey = "notificationId";

            // Act & Assert
            Assert.Throws<FormatException>(() => PartitionKeyUtility.GetNotificationIdFromBatchPartitionKey(batchPartitionKey));
        }
    }
}