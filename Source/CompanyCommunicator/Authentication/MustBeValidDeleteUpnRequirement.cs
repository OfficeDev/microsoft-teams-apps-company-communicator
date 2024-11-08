﻿// <copyright file="MustBeValidDeleteUpnRequirement.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// This class is an authorization policy requirement.
    /// It specifies that an id token must contain Upn claim.
    /// </summary>
    public class MustBeValidDeleteUpnRequirement : IAuthorizationRequirement
    {
    }
}
