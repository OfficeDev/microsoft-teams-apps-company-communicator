// <copyright file="StatusReasonExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Extensions
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Newtonsoft.Json;

    /// <summary>
    /// Extensions for status reason.
    /// </summary>
    public static class StatusReasonExtension
    {
        /// <summary>
        /// adds the status code to error message.
        /// </summary>
        /// <param name="errorMessage">the error message.</param>
        /// <param name="statusCode">the status code.</param>
        /// <returns>status code appended error message.</returns>
        public static string AddStatusCode(this string errorMessage, string statusCode)
        {
            string result;
            if (string.IsNullOrEmpty(errorMessage))
            {
                result = "OK";
            }
            else if (errorMessage.Contains("error"))
            {
                var rootMessage = JsonConvert.DeserializeObject<RootErrorMessage>(errorMessage);
                result = rootMessage.Error.Message;
            }
            else
            {
                result = errorMessage;
            }

            return $"{statusCode} : {result}";
        }
    }
}
