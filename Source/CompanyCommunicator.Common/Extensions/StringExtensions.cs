// <copyright file="StringExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extension class for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Append new line to the original string.
        /// </summary>
        /// <param name="originalString">the original string.</param>
        /// <param name="newString">the string to be appended.</param>
        /// <returns>the appended string.</returns>
        public static string AppendNewLine(this string originalString, string newString)
        {
            return string.IsNullOrEmpty(newString)
                ? originalString
                : string.IsNullOrWhiteSpace(originalString)
                ? newString
                : $"{originalString}{Environment.NewLine}{newString}";
        }

        /// <summary>
        /// Adds spaces between camel-case characters in a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>A new string with spaces added between camel-case characters.</returns>
        public static string AddSpacesToCamelCase(this string input)
        {
            string pattern = @"(?<=[a-z])(?=[A-Z])";
            string replacement = " ";
            string result = Regex.Replace(input, pattern, replacement);
            return result;
        }
    }
}
