// <copyright file="MustBeValidUpnRequirement.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Authentication
{
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// This class is an authorization policy requirement.
    /// It specifies that an id token must contain Upn claim.
    /// </summary>
    public class MustBeValidUpnRequirement : IAuthorizationRequirement
    {
    }
}
