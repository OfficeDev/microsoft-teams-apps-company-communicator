// <copyright file="StringExtensionsTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Extensions
{
    using System;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Xunit;

    /// <summary>
    /// String extension tests.
    /// </summary>
    public class StringExtensionsTest
    {
        /// <summary>
        /// Test case to check if new string is appended to original string.
        /// </summary>
        [Fact]
        public void Check_AppendNewLine_ShouldBeSuccess()
        {
            string original = "foo";
            string newString = "bar";
            var actualString = $"{original}{Environment.NewLine}{newString}";
            var expectedString = original.AppendNewLine(newString);

            Assert.Equal(expectedString, actualString);
        }

        /// <summary>
        /// Test case to check if original string is returned if empty string is tried to be appended.
        /// </summary>
        [Fact]
        public void AddEmptyString_AppendNewLine_ShouldBeSuccess()
        {
            string original = "foo";
            string newString = string.Empty;
            var expectedString = original.AppendNewLine(newString);

            Assert.Equal(expectedString, original);
        }

        /// <summary>
        /// Test case to check if new string is returned in case original string is empty.
        /// </summary>
        [Fact]
        public void AddOnEmptyString_AppendNewLine_ShouldBeSuccess()
        {
            string original = string.Empty;
            string newString = "bar";
            var expectedString = original.AppendNewLine(newString);

            Assert.Equal(expectedString, newString);
        }
    }
}
