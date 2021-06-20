// <copyright file="UserType.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    /// <summary>
    /// This represents the User Type property of User entity in Microsoft Graph.
    /// Ref : https://docs.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0.
    /// The values should be kept exactly the same to the values of userType property.
    /// </summary>
    public class UserType
    {
        /// <summary>
        /// This represents Member value of userType property of User entity in Microsoft Graph.
        /// </summary>
        public const string Member = "Member";

        /// <summary>
        /// This represents Guest value of userType property of User entity in Microsoft Graph.
        /// </summary>
        public const string Guest = "Guest";
    }
}
