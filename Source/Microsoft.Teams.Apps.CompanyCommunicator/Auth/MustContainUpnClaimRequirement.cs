// <copyright file="MustContainUpnClaimRequirement.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Auth
{
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// This class is an authorization policy requirement.
    /// It specifies that an id token must contain Upn claim.
    /// </summary>
    public class MustContainUpnClaimRequirement : IAuthorizationRequirement
    {
    }
}
