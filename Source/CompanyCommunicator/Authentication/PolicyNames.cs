﻿// <copyright file="PolicyNames.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    /// <summary>
    /// This class lists the names of the custom authorization policies in the project.
    /// </summary>
    public class PolicyNames
    {
        /// <summary>
        /// The name of the authorization policy, MustHaveUpnClaimPolicy.
        /// </summary>
        public const string MustBeValidUpnPolicy = "MustBeValidUpnPolicy";

        /// <summary>
        /// The name of the authorization policy, MSGraphGroupDataPolicy.
        /// </summary>
        public const string MSGraphGroupDataPolicy = "MSGraphGroupDataPolicy";

        /// <summary>
        /// The name of the authorization policy, MustHaveDeleteUpnClaimPolicy.
        /// </summary>
        public const string MustBeValidDeleteUpnPolicy = "MustBeValidDeleteUpnPolicy";
    }
}
